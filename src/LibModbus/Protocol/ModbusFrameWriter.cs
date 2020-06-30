using System;
using System.Buffers;
using System.Buffers.Binary;
using LibModbus.Frame;

namespace LibModbus.Protocol
{
    internal sealed class ModbusFrameWriter
    {
        private const byte HEADER_LEN = 7;
        private const ushort PROTOCOL_ID = 0;
        private readonly IBufferWriter<byte> _writer;

        public ModbusFrameWriter(IBufferWriter<byte> writer)
        {
            _writer = writer;
        }

        public int WriteFrame(RequestAdu frame) => frame.Pdu switch
        {
            RequestReadCoils request => WriteRequestReadCoils(frame.Header, request),
            RequestWriteSingleCoil request => WriteRequestWriteSingleCoil(frame.Header, request),
            RequestWriteMultipleCoils request => WriteRequestWriteMultipleCoils(frame.Header, request),
            _ => 0,
        };

        private int WriteHeader(Memory<byte> memory, ushort transactionID, byte unitID, ushort length)
        {
            var written = 0;
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), transactionID);

            // Write Protocol ID
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), PROTOCOL_ID);

            // Write length
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), length);

            // Write Unit ID
            written += WriteByte(memory.Span.Slice(written, 1), unitID);

            return written;
        }

        private int WriteRequestReadCoils(Header header, RequestReadCoils request)
        {
            var memory = _writer.GetMemory(HEADER_LEN + sizeof(ushort) * 2 + sizeof(byte));

            var written = WriteHeader(memory, header.TransactionID, header.UnitID, 6);

            // Write Function Code
            written += WriteByte(memory.Span.Slice(written, 1), (byte)ModbusFunction.ReadCoilStatus);

            // Write Address Data
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), request.Address);

            // Write Quantity Data
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), request.Quantity);

            return written;
        }

        private int WriteRequestWriteSingleCoil(Header header, RequestWriteSingleCoil request)
        {
            var memory = _writer.GetMemory(HEADER_LEN + sizeof(ushort) * 2 + sizeof(byte));

            var written = WriteHeader(memory, header.TransactionID, header.UnitID, 6);

            // Write Function Code
            written += WriteByte(memory.Span.Slice(written, 1), (byte)ModbusFunction.WriteSingleCoil);
            // Write Address Data
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), request.Address);

            // Write State
            var data = ModbusFrameUtils.BoolToCoil(request.CoilState);
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), data);

            return written;
        }

        private int WriteRequestWriteMultipleCoils(Header header, RequestWriteMultipleCoils request)
        {
            var memory = _writer.GetMemory(256);
            var byteCount = ModbusFrameUtils.GetByteCount(request.CoilStates);
            var length = (ushort)(HEADER_LEN + byteCount);

            var written = WriteHeader(memory, header.TransactionID, header.UnitID, length);

            // Write Function Code
            written += WriteByte(memory.Span.Slice(written, 1), (byte)ModbusFunction.WriteMultipleCoils);

            // Write Address Data
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), request.Address);;

            // Write Number of registers
            written += WriteUInt16BigEndian(memory.Span.Slice(written, 2), (ushort)request.CoilStates.Length);

            // Write byte count
            written += WriteByte(memory.Span.Slice(written, 1), (byte)byteCount);

            // Write packed coil values
            written += ModbusFrameUtils.PackCoils(memory.Span.Slice(written), request.CoilStates);

            return written;
        }

        private static int WriteByte(Span<byte> buffer, byte value)
        {
            buffer[0] = value;
            return sizeof(byte);
        }

        private static int WriteUInt16BigEndian(Span<byte> buffer, ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
            return sizeof(ushort);
        }
    }
}
