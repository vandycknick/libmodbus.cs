using LibModbus.Protocol;
using LibModbus.Frame;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LibModbus.Transport;

namespace LibModbus
{
    /// <summary>
    /// Modbus TCP Client
    /// </summary>
    public sealed class ModbusClient : IAsyncDisposable
    {
        private const byte UNIT_ID = 0xFE;

        private int _transactionId = 0;
        private bool _isDisposed;
        private int _timeout = 1000;

        private IConnection _connection;
        private ModbusFrameWriter _writer;
        private Task _readingTask;
        private IConnectionFactory _factory;

        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<ResponseAdu>> _messages = new ConcurrentDictionary<ushort, TaskCompletionSource<ResponseAdu>>();

        internal ModbusClient(IConnectionFactory factory)
        {
            _factory = factory;
        }   

        public async Task ConnectAsync(CancellationToken token = default)
        {
            _connection = await _factory.ConnectAsync(token).ConfigureAwait(false);

            _writer = new ModbusFrameWriter(_connection.Transport.Output);

            _readingTask = ReadMessages();
        }

        public async Task<List<bool>> ReadCoils(ushort address, ushort quantity, CancellationToken token = default)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var header = CreateHeader();
            var frame = new RequestAdu
            {
                Header = header,
                Pdu = new RequestReadCoils
                {
                    Address = address,
                    Quantity = quantity,
                }
            };

            var written = _writer.WriteFrame(frame);
            _connection.Transport.Output.Advance(written);

            var waitForResponse = WaitForResponse<ResponseReadCoils>(frame, token).ConfigureAwait(false);
            var flushResult = await _connection.Transport.Output.FlushAsync(token).ConfigureAwait(false);

            var response = await waitForResponse;

            var bits = new BitArray(response.Coils);
            var result = new List<bool>();

            for (var i = 0; i < quantity; i++)
            {
                result.Add(bits[i]);
            }

            return result;
        }

        public async Task<bool> WriteSingleCoil(ushort address, bool state, CancellationToken token = default)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

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

            _connection.Transport.Output.Advance(written);

            var waitForResponse = WaitForResponse<ResponseWriteSingleCoil>(frame, token).ConfigureAwait(false);

            var result = await _connection.Transport.Output.FlushAsync(token).ConfigureAwait(false);

            var response = await waitForResponse;

            return response.Result;
        }

        public async Task WriteMultipleCoils(ushort address, bool[] states, CancellationToken token = default)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var header = CreateHeader();
            var frame = new RequestAdu
            {
                Header = header,
                Pdu = new RequestWriteMultipleCoils
                {
                    Address = address,
                    CoilStates = states
                }
            };

            var written = _writer.WriteFrame(frame);

            _connection.Transport.Output.Advance(written);

            var waitForResponse = WaitForResponse<ResponseWriteMultipleCoils>(frame, token).ConfigureAwait(false);

            var result = await _connection.Transport.Output.FlushAsync(token).ConfigureAwait(false);

            var response = await waitForResponse;

            // TODO: check if quantity and adress are correct?
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

        private SequencePosition ReadFrame(ReadOnlySequence<byte> buffer)
        {
            var reader = new ModbusFrameReader(buffer);
            var position = reader.ReadFrame(out var frame);

            if (!frame.Equals(ResponseAdu.Empty) &&
                _messages.TryRemove(frame.Header.TransactionID, out var source) && frame.Header.UnitID == UNIT_ID)
            {
                source.SetResult(frame);
            }

            return position;
        }

        private async Task ReadMessages()
        {
            while (true)
            {
                var result = await _connection.Transport.Input.ReadAsync().ConfigureAwait(false);

                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }

                var buffer = result.Buffer;
                var position = ReadFrame(buffer);

                _connection.Transport.Input.AdvanceTo(position, buffer.End);
            }
        }

        private Header CreateHeader() => new Header(NextTransactionID(), UNIT_ID);

        private async Task<T> WaitForResponse<T>(RequestAdu request, CancellationToken token = default) where T : IResponsePdu
        {
            var header = request.Header;
            var source = new TaskCompletionSource<ResponseAdu>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_messages.TryAdd(header.TransactionID, source))
            {
                throw new InvalidOperationException("Can't start transaction.");
            }

            var race = await Task.WhenAny(source.Task, Task.Delay(_timeout, token)).ConfigureAwait(false);

            if (race == source.Task)
            {
                var responseFrame = await source.Task;

                if (responseFrame.Pdu is T response)
                {
                    return response;

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
            if (_isDisposed)
            {
                ThrowObjectDisposedException();
            }
        }

        private void ThrowIfNotConnected()
        {
            if (_connection == null) throw new SocketException((int)SocketError.NotConnected);
        }

        [DoesNotReturn]
        private void ThrowObjectDisposedException() => throw new ObjectDisposedException(GetType().FullName);

        public async ValueTask DisposeAsync()
        {
            _isDisposed = true;

            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }

            if (_readingTask != null)
            {
                try
                {
                    await _readingTask;
                    _readingTask.Dispose();
                }
                catch 
                {}
            }
        }
    }
}
