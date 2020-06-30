using LibModbus.Transport;
using LibModbus.Transport.Serial;
using LibModbus.Transport.Sockets;
using System;
using System.IO.Ports;
using System.Net;

namespace LibModbus
{
    public class ModbusClientBuilder
    {
        private IConnectionFactory _factory;

        public ModbusClientBuilder UseSocket(string address, int port = 502)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            return UseSocket(new IPEndPoint(IPAddress.Parse(address), port));
        }

        public ModbusClientBuilder UseSocket(EndPoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (_factory != null)
            {
                throw new InvalidOperationException("Multiple connections not supported!");
            }

            _factory = new SocketConnectionFactory(endpoint);
            return this;
        }

        // NOT STABLE YET
        // public ModbusClientBuilder UseSerial(string portname)
        // {
        //     if (portname == null)
        //     {
        //         throw new ArgumentNullException(nameof(portname));
        //     }

        //     if (_factory != null)
        //     {
        //         throw new InvalidOperationException("Multiple connections not supported!");
        //     }

        //     _factory = new SerialConnectionFactory(portname);
        //     return this;
        // }

        public ModbusClient Build() => new ModbusClient(_factory);
    }
}
