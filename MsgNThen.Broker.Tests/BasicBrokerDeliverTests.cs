using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using Xunit;

namespace MsgNThen.Broker.Tests
{
    public class BasicBrokerDeliverTests
    {
        [Fact]
        public async Task TestS3()
        {
            var sp = TestHelpers.CreateServiceProvider();
            var deliverer = sp.GetService<IUriDeliveryBroker>();
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("Hi There"));

            await deliverer.Deliver(new Uri("s3://Default@msgnthen/tests/{0}"), new MsgNThenMessage()
            {
                Body = stream,
                Headers = new HeaderDictionary() {{HeaderConstants.MessageId, new StringValues("Test1")}},
                ReasonPhrase = "OK",
                StatusCode = 200
            });
        }

        [Fact]
        public async Task TestRabbit()
        {
            var sp = TestHelpers.CreateServiceProvider();
            var deliverer = sp.GetService<IUriDeliveryBroker>();
            var publisher = sp.GetService<IMessagePublisher>();
            var listener = sp.GetService<IRabbitMqListener>();
            var oneMessageHandler = (OneMessageHandler) sp.GetService<IMessageHandler>();

            var msgGuid = Guid.NewGuid();
            var messageBody = msgGuid.ToByteArray();
            var stream = new MemoryStream(messageBody);

            var queueName = "tests";
            var routingKey = "testrabbit";
            publisher.BindDirectQueue("msgnthen", queueName, routingKey);

            await deliverer.Deliver(new Uri($"rabbit://msgnthen/{routingKey}"), new MsgNThenMessage()
            {
                Body = stream,
                Headers = new HeaderDictionary() { { HeaderConstants.MessageId, new StringValues("Test1") } },
                ReasonPhrase = "OK",
                StatusCode = 200
            });

            listener.Listen(queueName, 2);

            for (int i = 0; i < 2000; i++)
            {
                var msg = oneMessageHandler.PopMessage();
                if (msg != null)
                {
                    if (msg.Body.Length == messageBody.Length)
                    {
                        var res = new Guid(msg.Body);
                        if (msgGuid == res)
                        {
                            //yey: we found the message we sent.
                            return;
                        }
                    }
                }
                Thread.Sleep(200);
            }
            Assert.False(true, "did not find expected message.");

        }
    }
}
