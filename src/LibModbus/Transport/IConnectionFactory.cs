using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LibModbus.Transport
{
    interface IConnectionFactory
    {
        ValueTask<IConnection> ConnectAsync(CancellationToken cancellationToken = default);
    }
}
