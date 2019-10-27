using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using MsgNThen.Interfaces;

namespace MsgNThen.Adapter
{
    class MsgNThenApdapterFactory : IMsgNThenApdapterFactory, IMessageHandler
    {
        private readonly ILogger<MsgNThenApdapterFactory> _logger;
        private IMsgNThenHttpAdapter _msgNThenHttpAdapter;

        public MsgNThenApdapterFactory(ILogger<MsgNThenApdapterFactory> logger)
        {
            _logger = logger;
        }

        public IMsgNThenHttpAdapter Start<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            if (_msgNThenHttpAdapter != null)
            {
                //todo: create MsgNThen core exception
                throw new Exception("MsgNThenApdapterFactory.Start called more than once.");
            }
            _msgNThenHttpAdapter = new MsgNThenHttpAdapter<TContext>(_logger, application, cancellationToken);
            return _msgNThenHttpAdapter;
        }

        public Task<AckResult> HandleMessageTask(IHandledMessage message)
        {
            return _msgNThenHttpAdapter.HandleMessageTask(message);
        }
    }
}