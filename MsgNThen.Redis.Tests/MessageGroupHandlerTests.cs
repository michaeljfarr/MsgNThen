using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MsgNThen.Interfaces;
using MsgNThen.Redis.Abstractions;
using MsgNThen.Redis.NThen;
using NSubstitute;
using Xunit;

namespace MsgNThen.Redis.Tests
{
    public class MessageGroupHandlerTests
    {
        [Fact]
        public void TestAndThenDelivery()
        {
            var serviceCollection = RedisTaskFunnelsTests.CreateServiceCollection();
            var andThenDeliverer = Substitute.For<IAndThenMessageDeliverer>();
            var andThenMessages = new ConcurrentQueue<SimpleMessage>();
            andThenDeliverer.When(a => a.Deliver(Arg.Any<SimpleMessage>()))
                .Do(x =>
                {
                    andThenMessages.Enqueue(x.ArgAt<SimpleMessage>(0));
                });
            serviceCollection.AddSingleton(andThenDeliverer);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var messageGroupHandler = serviceProvider.GetRequiredService<IMessageGroupHandler>();
            var groupId = Guid.NewGuid();
            var messageIds = new[] {Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()};
            messageGroupHandler.StartMessageGroup(groupId);
            messageGroupHandler.MessagesPrepared(groupId, messageIds);
            var msg = new SimpleMessage() {Body = new[] {(byte) 'a'}};
            messageGroupHandler.SetMessageGroupCount(groupId, messageIds.Length, msg);
            messageGroupHandler.CompleteMessageGroupTransmission(groupId);
            foreach (var messageId in messageIds.Take(messageIds.Length - 1))
            {
                messageGroupHandler.MessageHandled(groupId, messageId);
                messageGroupHandler.MessageHandled(groupId, messageId);
            }
            //andThenMessage should not be sent until the last message is delivered
            andThenMessages.Count.Should().Be(0);
            messageGroupHandler.MessageHandled(groupId, messageIds.Last());
            andThenMessages.Count.Should().Be(1);
            andThenMessages.TryDequeue(out msg).Should().BeTrue();
            msg.Body.Should().NotBeEmpty();
            msg.Body[0].Should().Be((byte) 'a');

            //sending the last message again should not (in this configuration) send another andthen message;
            andThenMessages.Count.Should().Be(0);
            messageGroupHandler.MessageHandled(groupId, messageIds.Last());
            andThenMessages.Count.Should().Be(0);
        }

    }
}
