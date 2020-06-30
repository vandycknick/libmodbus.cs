using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LibModbus.Transport.Serial
{
    internal class SerialConnectionFactory : IConnectionFactory
    {
        private readonly string _portname;
        private readonly int _baudRate;
        private readonly Parity _parity;
        private readonly int _dataBits;
        private readonly StopBits _stopBits;
        private readonly Handshake _handshake;

        public SerialConnectionFactory(
            string portname = SerialConnection.DefaultPortName, int baudRate = SerialConnection.DefaultBaudRate,
            Parity parity = SerialConnection.DefaultParity, int dataBits = SerialConnection.DefaultDataBits,
            StopBits stopBits = SerialConnection.DefaultStopBits, Handshake handshake = SerialConnection.DefaultHandshake)
        {
            _portname = portname;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _handshake = handshake;
        }

        public ValueTask<IConnection> ConnectAsync(CancellationToken cancellationToken = default)
        {
            return new SerialConnection(_portname, _baudRate, _parity, _dataBits, _stopBits, _handshake).StartAsync();
        }
    }
}
