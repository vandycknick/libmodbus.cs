using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LibModbus.Protocol;
using LibModbus.Frame;
using Pipelines.Sockets.Unofficial;

namespace LibModbus
{
    /// <summary>
    ///     Modbus TCP Client
    /// </summary>
    public sealed class ModbusClient : IDisposable
    {
        private int _transactionId = 0;
        private byte _unitId;
        private bool disposedValue;

        private int _timeout = 500000;
        // private ushort _refresh = 10;

        private readonly SocketConnection _connection;
        private readonly EndPoint _endpoint;
        private readonly ModbusFrameWriter _writer;

        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<ResponseAdu>> _messages = new ConcurrentDictionary<ushort, TaskCompletionSource<ResponseAdu>>();

        public ModbusClient(string address, int port = 502) : this(new IPEndPoint(IPAddress.Parse(address), port))
        {

        }

        public ModbusClient(EndPoint endpoint)
        {
            var options = new PipeOptions();
            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            var random = new Random();
            _unitId = (byte)random.Next(byte.MinValue + 1, byte.MaxValue);

            _endpoint = endpoint;
            _connection = SocketConnection.Create(socket, options);

            _writer = new ModbusFrameWriter(_connection.Output);
        }

        private ushort NextTransactionID()
        {
            if (_transactionId == ushort.MaxValue)
            {
                Interlocked.Exchange(ref _transactionId, ushort.MinValue);
                return ushort.MinValue;
            }
            else
            {
                return (ushort)Interlocked.Increment(ref _transactionId);
            }
        }

        public async ValueTask Connect()
        {
            if (_connection.Socket.Connected) return;

            await _connection.Socket.ConnectAsync(_endpoint).ConfigureAwait(false);
            Console.WriteLine($"Connection status {_connection.Socket.Connected}");
            _ = PollMessages();
        }

        private SequencePosition ReadFrame(ReadOnlySequence<byte> buffer)
        {
            var reader = new ModbusFrameReader(buffer);
            var position = reader.ReadFrame(out var frame);

            if (!frame.Equals(ResponseAdu.Empty) &&
                _messages.TryRemove(frame.Header.TransactionID, out var source) && frame.Header.UnitID == _unitId)
            {
                source.SetResult(frame);
            }

            return position;
        }

        private async Task PollMessages()
        {
            try
            {
                while (_connection.Socket.Connected)
                {
                    var result = await _connection.Input.ReadAsync().ConfigureAwait(false);
                    var buffer = result.Buffer;
                    var position = ReadFrame(buffer);

                    _connection.Input.AdvanceTo(position, buffer.End);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void ThrowIfNotConnected()
        {
            if (!_connection.Socket.Connected)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }
        }

        private Header CreateHeader() => new Header(NextTransactionID(), _unitId);

        public async Task<List<bool>> ReadCoils(ushort address, ushort quantity, CancellationToken token = default)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            var header = CreateHeader();
            var request = new RequestAdu
            {
                Header = header,
                Pdu = new RequestReadCoils
                {
                    Address = address,
                    Quantity = quantity,
                }
            };

            var written = _writer.WriteFrame(request);
            _connection.Output.Advance(written);

            var flushResult = await _connection.Output.FlushAsync(token).ConfigureAwait(false);

            var source = new TaskCompletionSource<ResponseAdu>();

            if (!_messages.TryAdd(header.TransactionID, source))
            {
                // TODO: improve this
                throw new Exception("Can't start transaction");
            }

            var race = await Task.WhenAny(source.Task, Task.Delay(_timeout, token)).ConfigureAwait(false);

            if (race == source.Task)
            {
                var frame = await source.Task;

                if (frame.Pdu is ResponseReadCoils response)
                {
                    var bits = new BitArray(response.Coils);
                    var result = new List<bool>();

                    for (var i = 0; i < quantity; i++)
                    {
                        result.Add(bits[i]);
                    }

                    return result;
                }
                else if (frame.Pdu is ResponseError error)
                {
                    throw new ModbusRequestException(error.ErrorCode);
                }
                else
                {
                    throw new ModbusRequestException("Unknown response");
                }
            }
            else
            {
                source.SetCanceled();
                _messages.Remove(header.TransactionID, out var _);
                throw new TimeoutException();
            }
        }

        public async Task<bool> WriteSingleCoil(ushort address, bool state, CancellationToken token = default)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            var header = CreateHeader();
            var frame = new RequestAdu
            {
                Header = header,
                Pdu = new RequestWriteSingleCoil
                {
                    Address = address,
                    CoilState = state,
                }
            };

            var written = _writer.WriteFrame(frame);

            _connection.Output.Advance(written);

            var flushResult = await _connection.Output.FlushAsync(token).ConfigureAwait(false);

            var source = new TaskCompletionSource<ResponseAdu>();


            if (!_messages.TryAdd(header.TransactionID, source))
            {
                throw new InvalidOperationException("Can't start transaction.");
            }

            var race = await Task.WhenAny(source.Task, Task.Delay(_timeout, token)).ConfigureAwait(false);

            if (race == source.Task)
            {
                var responseFrame = await source.Task;
                
                if (responseFrame.Pdu is ResponseWriteSingleCoil response)
                {
                    return response.Result;

                }
                else if (responseFrame.Pdu is ResponseError error)
                {
                    throw new ModbusRequestException(error.ErrorCode);
                }
                else
                {
                    throw new ModbusRequestException("Unknown response");
                }
            }
            else
            {
                source.SetCanceled();
                _messages.Remove(header.TransactionID, out var _);
                throw new TimeoutException();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposedValue)
            {
                ThrowObjectDisposedException();
            }
        }

        [DoesNotReturn]
        private void ThrowObjectDisposedException() => throw new ObjectDisposedException(GetType().FullName);

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                _connection.Dispose();
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        ~ModbusClient()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
