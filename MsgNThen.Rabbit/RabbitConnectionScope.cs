using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace MsgNThen.Rabbit
{
    /// <summary>
    /// RabbitConnectionScope will be disposed with the Dependency Injection scope and
    /// will close the connection with ReplySuccess if the connection is not already closed.
    /// </summary>
    class RabbitConnectionScope : IDisposable
    {
        private readonly IConnection _connection;

        public RabbitConnectionScope(IConnection connection)
        {
            _connection = connection;
        }
        

        public void Dispose()
        {
            if (_connection.IsOpen)
            {
                _connection.Close(Constants.ReplySuccess, "Closing the connection");
            }
        }
    }
}