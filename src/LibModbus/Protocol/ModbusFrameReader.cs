using System;
using System.Buffers;
using System.Buffers.Binary;
using LibModbus.Frame;

namespace LibModbus.Protocol
{
    internal ref struct ModbusFrameReader
    {
        private const byte HEADER_LENGTH = 7;
        private const byte ERROR_BIT = 0x80;
        private ReadOnlySequence<byte> _sequence;

        public ModbusFrameReader(ReadOnlySequence<byte> sequence)
        {
            _sequence = sequence;
        }

        public SequencePosition ReadFrame(out ResponseAdu frame)
        {
            frame = ResponseAdu.Empty;
            var position = _sequence.Start;

            if (TryParseHeader(ref _sequence, out var header, out var length))
            {
                // Decrease length by one because unitID is part of the lenght but already parsed in the header
                var dataLen = length - 1;
                if (TryReadResponse(ref _sequence, (ushort)dataLen, out var response))
                {
                    frame = new ResponseAdu
                    {
                        Header = header,
                        Pdu = response,
                    };
                    return _sequence.Start;
                }
                else
                {
                    return position;
                }

            }

            return _sequence.Start;
        }

        private bool TryParseHeader(ref ReadOnlySequence<byte> buffer, out Header header, out ushort length)
        {
            header = Header.Empty;
            length = 0;

            if (buffer.Length < HEADER_LENGTH)
            {
                return false;
            }

            // Grab the first 7 bytes of the buffer
            var lengthSlice = buffer.Slice(buffer.Start, HEADER_LENGTH);
            var result = false;
            if (lengthSlice.IsSingleSegment)
            {
                // Fast path since it's a single segment
                result = TryParseHeader(lengthSlice.First.Span, out header, out length);
            }
            else
            {
                // We have 7 bytes split across multiple segments, since it's so small we can copy it to a
                // stack allocated buffer, this avoids a heap allocation.
                Span<byte> stackBuffer = stackalloc byte[HEADER_LENGTH];
                lengthSlice.CopyTo(stackBuffer);
                result = TryParseHeader(stackBuffer, out header, out length);
            }

            buffer = buffer.Slice(lengthSlice.End);
            return result;
        }

        private static bool TryParseHeader(ReadOnlySpan<byte> data, out Header header, out ushort length)
        {
            var id = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(0, 2));
            var protocol = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2, 2));
            length = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(4, 2));
            var unitId = data[6];

            if (protocol == 0)
            {
                header = new Header(id, unitId);
                return true;
            }

            header = Header.Empty;
            return false;
        }

        private static bool TryReadResponse(ref ReadOnlySequence<byte> buffer, ushort length, out IResponsePdu response)
        {
            response = null;

            if (buffer.Length < length)
            {
                return false;
            }

            var lengthSlice = buffer.Slice(buffer.Start, length);
            var result = false;
            if (lengthSlice.IsSingleSegment)
            {
                // Fast path since it's a single segment
                result = TryReadResponse(lengthSlice.First.Span, out response);
            }
            else if (lengthSlice.Length < 256)
            {
                // It should be safe to just allocate on the stack
                Span<byte> stackBuffer = stackalloc byte[length];
                lengthSlice.CopyTo(stackBuffer);
                result = TryReadResponse(stackBuffer, out response);
            }
            else
            {
                // Too big to allocate on the stack, let's use an arraypool
                var tmpBuffer = ArrayPool<byte>.Shared.Rent(length);
                lengthSlice.CopyTo(tmpBuffer);
                result = TryReadResponse(tmpBuffer, out response);
                ArrayPool<byte>.Shared.Return(tmpBuffer);
            }

            buffer = buffer.Slice(lengthSlice.End);
            return result;
        }

        private static byte[] ReadCoilData(ReadOnlySpan<byte> data) => data.Slice(1, data[0]).ToArray();

        private static ushort[] ReadWordData(ReadOnlySpan<byte> data)
        {
            var length = data[0];
            var registers = length / 2;
            var words = new ushort[registers];
            var registerSlice = data.Slice(1);

            for (var i = 0; i < registers; i++)
            {
                words[i] = BinaryPrimitives.ReadUInt16BigEndian(registerSlice.Slice(i  * 2, 2));
            }

            return words;
        }

        private static bool TryReadResponse(ReadOnlySpan<byte> data, out IResponsePdu response)
        {
            var code = data[0];

            switch (code)
            {
                case (byte)ModbusFunction.ReadCoils:
                    {
                        response = new ResponseReadCoils
                        {
                            Coils = ReadCoilData(data.Slice(1)),
                        };
                        return true;
                    }

                case (byte)ModbusFunction.ReadDiscreteInputs:
                    {
                        response = new ResponseReadDiscreteInputs
                        {
                            Coils = ReadCoilData(data.Slice(1)),
                        };
                        return true;
                    }

                case (byte)ModbusFunction.ReadInputRegisters:
                    {
                        response = new ResponseReadInputRegisters
                        {
                            Results = ReadWordData(data.Slice(1)),
                        };
                        return true;
                    }

                case (byte)ModbusFunction.ReadHoldingRegisters:
                    {
                        response = new ResponseReadHoldingRegisters
                        {
                            Results = ReadWordData(data.Slice(1)),
                        };

                        return true;
                    }

                case (byte)ModbusFunction.WriteSingleCoil:
                    {
                        var address = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(1, 2));
                        var coil = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(3, 2));

                        response = new ResponseWriteSingleCoil
                        {
                            Address = address,
                            Result = ModbusFrameUtils.CoilToBool(coil),
                        };
                        return true;
                    }

                case (byte)ModbusFunction.WriteMultipleCoils:
                    {
                        var address = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(1, 2));
                        var quantity = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(3, 2));

                        response = new ResponseWriteMultipleCoils
                        {
                            Address = address,
                            Quantity = quantity,
                        };
                        return true;
                    }

                case (byte)ModbusFunction.ReadCoils | ERROR_BIT:
                case (byte)ModbusFunction.ReadDiscreteInputs | ERROR_BIT:
                case (byte)ModbusFunction.ReadInputRegisters | ERROR_BIT:
                case (byte)ModbusFunction.ReadHoldingRegisters | ERROR_BIT:
                case (byte)ModbusFunction.WriteSingleCoil | ERROR_BIT:
                case (byte)ModbusFunction.WriteMultipleCoils | ERROR_BIT:
                    {
                        response = new ResponseError
                        {
                            ErrorCode = (ModbusErrorCode)data[1],
                        };
                        return true;
                    }

                default:
                    response = null;
                    return false;
            }
        }
    }
}
