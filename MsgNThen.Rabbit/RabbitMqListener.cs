﻿using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MsgNThen.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MsgNThen.Rabbit
{
    class RabbitMqListener : IRabbitMqListener
    {
        private readonly IConnection _connection;
        private readonly IMessageHandler _messageHandler;
        private readonly ILogger<RabbitMqListener> _logger;
        private readonly ConcurrentDictionary<ulong, Task> _runningTasks = new ConcurrentDictionary<ulong, Task>();
        private readonly ConcurrentDictionary<string, RabbitMqConsumer> _consumers = new ConcurrentDictionary<string, RabbitMqConsumer>();
        public RabbitMqListener(IConnection connection, IMessageHandler messageHandler, ILogger<RabbitMqListener> logger)
        {
            _connection = connection;
            _messageHandler = messageHandler;
            _logger = logger;
        }

        public int NumTasks => _runningTasks.Count;

        private RabbitMqConsumer CreateConsumerAndListen(string queueName, ushort maxThreads)
        {
            var consumer = CreateConsumer();
            consumer.SetThreadLimit(maxThreads);
            consumer.Listen(queueName);
            return consumer;
        }
        private RabbitMqConsumer CreateConsumer()
        {
            return new RabbitMqConsumer(_connection, this, _runningTasks, _logger);
        }

        public bool Listen(string queue, ushort maxThreads)
        {
            var consumer = ListenInner(queue, maxThreads);
            return consumer.IsOpen;
        }

        public void Remove(string queue)
        {
            if (_consumers.TryRemove(queue, out var consumer))
            {
                consumer.Stop();
            }
        }

        private RabbitMqConsumer ListenInner(string queue, ushort maxThreads)
        {
            return _consumers.AddOrUpdate(queue, a => CreateConsumerAndListen(queue, maxThreads), (q, c) =>
            {
                //todo: check how threadsafe this method is.
                c.SetThreadLimit(maxThreads);
                return c;
            });
        }

        private async Task<AckResult> Handle(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            await Task.Yield();
            var msgnThenMsgWrapper = new HandledMessageWrapper(basicDeliverEventArgs);
            return await _messageHandler.HandleMessageTask(msgnThenMsgWrapper);
        }


        /// <summary>
        /// RabbitMqConsumer wraps a single EventingBasicConsumer that listens to all queues as necessary. This is an unusual approach, so
        /// it is important to take note of the differences.  The intention is that a new task will be immediately created for each message
        /// that arrives, so that there will be up to {prefectCount} tasks at any point in time (because rabbit will not send any more unacked
        /// messages via https://www.rabbitmq.com/consumer-prefetch.html).
        ///
        /// It means that events across all queues are sent from a single thread, so it is critical for message
        /// throughput to ensure that all handlers yield immediately.
        ///
        /// The alternative approach 
        /// </summary>
        /// <remarks>
        /// RabbitMQ client uses a "thread-per-model" approach in the ConsumerWorkService.
        /// https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/251
        ///
        /// The autogenerated library method DispatchAsynchronous is called with a command that is interpreted as a new message
        /// HandleBasicDeliver on ModelBase is executed (via AutorecoveringModel) and the consumer is fetched by its tag.
        /// Methods on AsyncConsumerDispatcher/ConcurrentConsumerDispatcher are dispatched as an action to ConsumerWorkService (HandleBasicDeliver)
        /// Messages arrive at ConsumerWorkService and are sent to a work pool based on which model they are associated to.
        /// The work pool then dequeues each work item and invokes it synchronously and then blocks on an AutoResetEvent waiting for the next message
        /// </remarks>
        private class RabbitMqConsumer
        {
            private readonly RabbitMqListener _parent;
            private readonly ILogger _logger;
            private readonly IModel _channel;
            private readonly EventingBasicConsumer _consumer;
            private readonly ConcurrentDictionary<ulong, Task> _runningTasks;

            public RabbitMqConsumer(IConnection connection, RabbitMqListener parent, ConcurrentDictionary<ulong, Task> runningTasks, ILogger logger)
            {
                _parent = parent;
                _logger = logger;
                _runningTasks = runningTasks;
                _channel = connection.CreateModel();

                //the main difference with the Async Consumer is that it uses an async message loop, so it wont have much of 
                //an impact with this.
                _channel.BasicQos(0, 10, false);
                _consumer = new EventingBasicConsumer(_channel);
                _consumer.Received += Consumer_Received;
            }

            /// <summary>
            /// This should ensure that we have at most {maxThreads} threads at any one point from this consumer.
            /// Since we have setup one queues per consumer, rabbitmq server has an easy job to figure out how many
            /// unacked messages we have.
            ///
            /// The two downsides to occur if we want to subscribe to many queues
            /// - each model consumes its own thread (unless we use the async one?) that blocks on an AutoResetEvent
            ///     so this is obviously an expensive way to handle this, but at least that pain is on the client
            /// - say if it was 100 queues but we wanted a maximum of 50 active tasks,
            ///     we'd have to throttle our task execution within Consumer_Received with artificial delays (ew)
            /// </summary>
            /// <param name="maxThreads"></param>
            public void SetThreadLimit(ushort maxThreads)
            {
                _channel.BasicQos(0, maxThreads, false);
            }
            public void Listen(string queue)
            {
                _channel.BasicConsume(queue, false, _consumer);
            }

            public bool IsOpen => _channel.IsOpen;

            public bool Stop()
            {
                foreach (var consumerTag in _consumer.ConsumerTags)
                {
                    _channel.BasicCancel(consumerTag);
                }
                
                return !_consumer.IsRunning;// IsRunning should be false now.
            }

            /// <summary>
            /// ConsumerReceivedTask method is to ensure that the ack/nack is called in one way or another.  Since
            /// this will be called on a different thread, it is important to avoid ack/nacking multiple messages.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /// <returns></returns>
            private async Task ConsumerReceivedTask(object sender, BasicDeliverEventArgs e)
            {
                try
                {
                    var ackResult = await _parent.Handle(e);
                    switch (ackResult)
                    {
                        case AckResult.Ack:
                            _channel.BasicAck(e.DeliveryTag, false);
                            break;
                        case AckResult.NoAck:
                            break;
                        case AckResult.NackRequeue:
                            _channel.BasicNack(e.DeliveryTag, false, true);
                            break;
                        case AckResult.NackQuit:
                            _channel.BasicNack(e.DeliveryTag, false, false);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle message - discarding the message");
                    _channel.BasicNack(e.DeliveryTag, false, false);
                }
            }

            /// <summary>
            /// _runningTasks is currently only used for diagnostics.
            ///
            /// In the future we will use it to set an upper limit of waiting tasks and temporarily
            /// unsubscribe until the work dies down.  We may switch out this implement to use an
            /// interlocked.increment to just count the threads because ConcurrentDictionary
            /// may be too expensive for high load situations.
            /// </summary>
            private async Task ConsumerReceivedTaskHandler(object sender, BasicDeliverEventArgs e)
            {
                var task = ConsumerReceivedTask(sender, e);
                _runningTasks[e.DeliveryTag] = task;
                try
                {
                    await task;
                }
                finally
                {
                    _runningTasks.TryRemove(e.DeliveryTag, out var taskAgain);
                }
            }

            /// <summary>
            /// This method starts the task handler task which handles everything with async.
            /// The task returned from ConsumerReceivedTaskHandler is ignored so the calling thread
            /// is not blocked for long (it will blocked if the Task itself doesn't yield quickly or does
            /// other stupid things).
            /// </summary>
            private void Consumer_Received(object sender, BasicDeliverEventArgs e)
            {
                var _ = ConsumerReceivedTaskHandler(sender, e);
            }
        }
    }
}
