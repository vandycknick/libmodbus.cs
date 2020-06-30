using System;
using System.IO;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Threading.Tasks;

namespace LibModbus.Transport.Serial
{
    internal class SerialConnection : IConnection
    {
        internal const string DefaultPortName = "COM1";
        internal const int DefaultBaudRate = 9600;
        internal const Parity DefaultParity = Parity.None;
        internal const int DefaultDataBits = 8;
        internal const StopBits DefaultStopBits = StopBits.One;
        internal const Handshake DefaultHandshake = Handshake.None;

        private readonly SerialPort _serialPort;
        private volatile bool _aborted;
        private IDuplexPipe _application;

        public IDuplexPipe Transport { get; set; }
        public string ConnectionId { get; set; } = Guid.NewGuid().ToString();

        public SerialConnection(
            string portname = DefaultPortName, int baudRate = DefaultBaudRate,
            Parity parity = DefaultParity, int dataBits = DefaultDataBits,
            StopBits stopBits = DefaultStopBits, Handshake handshake = DefaultHandshake)
        {
            _serialPort = new SerialPort(portname, baudRate, parity, dataBits, stopBits);
            _serialPort.Handshake = handshake;
        }

        public ValueTask<IConnection> StartAsync()
        {
            _serialPort.Open();

            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

            Transport = pair.Transport;
            _application = pair.Application;

            return new ValueTask<IConnection>(this);
        }

        private async Task ExecuteAsync()
        {
            Exception sendError = null;
            try
            {
                var receiveTask = DoReceive();
                var sendTask = DoSend();

                // If the sending task completes then close the receive
                // We don't need to do this in the other direction because the kestrel
                // will trigger the output closing once the input is complete.
                if (await Task.WhenAny(receiveTask, sendTask).ConfigureAwait(false) == sendTask)
                {
                    // Tell the reader it's being aborted
                    _serialPort.Dispose();
                }

                // Now wait for both to complete
                await receiveTask;
                sendError = await sendTask;

                // Dispose the socket(should noop if already called)
                _serialPort.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception in {nameof(SerialConnection)}.{nameof(StartAsync)}: " + ex);
            }
            finally
            {
                // Complete the output after disposing the connection
                _application.Input.Complete(sendError);
            }
        }

        private async Task DoReceive()
        {
            Exception error = null;

            try
            {
                await ProcessReceives().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
            }
            finally
            {
                if (_aborted)
                {
                    error ??= new ConnectionAbortedException();
                }

                await _application.Output.CompleteAsync(error).ConfigureAwait(false);
            }
        }

        private async Task ProcessReceives()
        {
            while (true)
            {
                var buffer = _application.Output.GetMemory(1024);
                var bytesReceived = await _serialPort.BaseStream.ReadAsync(buffer);

                if (bytesReceived == 0)
                {
                    // FIN
                    break;
                }

                var flushTask = _application.Output.FlushAsync();

                if (!flushTask.IsCompleted)
                {
                    await flushTask.ConfigureAwait(false);
                }

                var result = flushTask.Result;
                if (result.IsCompleted)
                {
                    // Pipe consumer is shut down, do we stop writing
                    break;
                }
            }
        }

        private async Task<Exception> DoSend()
        {
            Exception error = null;

            try
            {
                await ProcessSends().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                error = null;
            }
            catch (IOException ex)
            {
                error = ex;
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
            }
            finally
            {
                _aborted = true;
                _serialPort.Close();
            }

            return error;
        }

        private async Task ProcessSends()
        {
            while (true)
            {
                // Wait for data to write from the pipe producer
                var result = await _application.Input.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;

                if (result.IsCanceled)
                {
                    break;
                }

                var end = buffer.End;
                var isCompleted = result.IsCompleted;
                if (!buffer.IsEmpty)
                {
                    if (buffer.IsSingleSegment)
                    {

                        await _serialPort.BaseStream.WriteAsync(buffer.First);
                    }
                    else
                    {
                        foreach (var sequence in buffer)
                        {
                            await _serialPort.BaseStream.WriteAsync(sequence);
                        }
                    }
                }

                _application.Input.AdvanceTo(end);

                if (isCompleted)
                {
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Transport != null)
            {
                await Transport.Output.CompleteAsync().ConfigureAwait(false);
                await Transport.Input.CompleteAsync().ConfigureAwait(false);
            }

            _serialPort.Dispose();
        }
    }
}
