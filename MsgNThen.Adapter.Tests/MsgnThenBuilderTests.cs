using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
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
    
    public class MsgnThenBuilderTests
    {
        public class TestContext
        {

        }

        [Fact]
        public async Task TestAppBuilder()
        {
            var serviceProvider = CreateServiceProvider();
            var messageGroupHandler = serviceProvider.GetRequiredService<IMessageGroupHandler>();
            var apdapterFactory = serviceProvider.GetRequiredService<IMsgNThenApdapterFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<MsgnThenBuilderTests>>();
            var httpApplication = Substitute.For<IHttpApplication<TestContext>>();
            var cancellationTokenSource = new CancellationTokenSource();
            var adapter = apdapterFactory.Start(httpApplication, cancellationTokenSource.Token);
            IHandledMessage message = new HandledMessageWrapper(new BasicDeliverEventArgs(
                "consumer", 1, false, "ex", "rout", new BasicProperties(), new byte[0]));
            var res = await adapter.HandleMessageTask(message);
            res.Should().Be(AckResult.Ack);
        }

        public static ServiceProvider CreateServiceProvider()
        {
            var collection = BasicIntegrationTests.CreateServiceCollection();
            
            return collection.BuildServiceProvider();
        }
    }
}