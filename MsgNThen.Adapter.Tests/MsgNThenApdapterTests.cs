using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsgNThen.Interfaces;
using MsgNThen.Rabbit;
using NSubstitute;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Xunit;

namespace MsgNThen.Adapter.Tests
{
    
    public class MsgNThenApdapterTests
    {
        public class DummyType
        {

        }

        [Fact]
        public async Task TestMessageProperties()
        {
            var serviceProvider = CreateServiceProvider();
            var apdapterFactory = serviceProvider.GetRequiredService<IMsgNThenApdapterFactory>();
            var httpApplication = Substitute.For<IHttpApplication<DummyType>>();
            
            var cancellationTokenSource = new CancellationTokenSource();
            var adapter = apdapterFactory.Start(httpApplication, cancellationTokenSource.Token);
            var body = new byte[0];
            IHandledMessage message = new HandledMessageWrapper(new BasicDeliverEventArgs(
                "consumer", 1, false, "ex", "rout", new BasicProperties(){MessageId = Guid.NewGuid().ToString()}, body));
            var res = await adapter.HandleMessageTask(message);
            res.Should().Be(AckResult.Ack);
            httpApplication.Received().CreateContext(Arg.Is<IFeatureCollection>(x =>
                CheckRequest(x, message, body)
            ));
        }

        private static bool CheckRequest(IFeatureCollection x, IHandledMessage message, byte[] body)
        {
            x.Get<IHttpRequestFeature>().Headers[HeaderConstants.MessageId].ToString().Should().Be( message.Properties.MessageId);
            ((MemoryStream)x.Get<IHttpRequestFeature>().Body).ToArray().Should().BeEquivalentTo(body);
            return true;
        }

        public static ServiceProvider CreateServiceProvider()
        {
            var collection = BasicIntegrationTests.CreateServiceCollection();
            
            return collection.BuildServiceProvider();
        }
    }
}