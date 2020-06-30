using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LibModbus.Transport.Sockets
{
    public class SocketConnectionFactory : IConnectionFactory
    {
        private readonly EndPoint _endpoint;

        public SocketConnectionFactory(EndPoint endpoint)
        {
            _endpoint = endpoint;
        }

        public ValueTask<IConnection> ConnectAsync(CancellationToken cancellationToken = default)
        {
            return new SocketConnection(_endpoint).StartAsync();
        }
    }
}
