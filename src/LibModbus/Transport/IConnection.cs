using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace LibModbus.Transport
{
    public interface IConnection : IAsyncDisposable
    {
        IDuplexPipe Transport { get; }
        string ConnectionId { get; }
        ValueTask<IConnection> StartAsync();
    }
}
