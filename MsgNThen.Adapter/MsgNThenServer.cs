using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using MsgNThen.Interfaces;

namespace MsgNThen.Adapter
{
    class MsgNThenServer : IServer
    {
        private readonly IMsgNThenApdapterFactory _apdapterFactory;
        private IMsgNThenHttpAdapter _httpAdapter;

        public MsgNThenServer(IMsgNThenApdapterFactory apdapterFactory)
        {
            _apdapterFactory = apdapterFactory;
            Features = new FeatureCollection();
        }

        public void Dispose()
        {
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            _httpAdapter = _apdapterFactory.Start<TContext>(application, cancellationToken);
            _httpAdapter.Listen();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _httpAdapter.StopListening();
            return Task.CompletedTask;
        }

        public IFeatureCollection Features { get; }
        
    }
}