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

        private int WriteReadCoilRequest(ushort transactionID, byte unitID, ushort address, ushort quantity)
        {
            var memory = _writer.GetMemory(HEADER_LEN + sizeof(ushort) * 2 + sizeof(byte));

            var written = WriteHeader(memory, transactionID, unitID, 6);

            // Write Function Code
            memory.Span[written] = (byte)ModbusFunction.ReadCoilStatus;
            written += sizeof(byte);

            // Write Address Data
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), address);
            written += sizeof(ushort);

            // Write Quantity Data
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), quantity);
            written += sizeof(ushort);

            return written;
        }

        private int WriteSetSingleCoilRequest(ushort transactionID, byte unitID, ushort address, bool state)
        {
            var memory = _writer.GetMemory(HEADER_LEN + sizeof(ushort) * 2 + sizeof(byte));

            var written = WriteHeader(memory, transactionID, unitID, 6);

            // Write Function Code
            memory.Span[written] = (byte)ModbusFunction.WriteSingleCoil;
            written += sizeof(byte);

            // Write Address Data
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), address);
            written += sizeof(ushort);

            // Write State
            var data = state ? 0xFF00 : 0x0000;
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), (ushort)data);
            written += sizeof(ushort);

            return written;
        }

        public int WriteFrame(RequestAdu frame)
        {
            switch (frame.Pdu)
            {
                case RequestReadCoils request:
                    return WriteRequestReadCoils(frame.Header, request);

                case RequestWriteSingleCoil request:
                    return WriteRequestWriteSingleCoil(frame.Header, request);

                default:
                    return 0;
            }
        }

        private int WriteRequestReadCoils(Header header, RequestReadCoils request)
        {
            return WriteReadCoilRequest(header.TransactionID, header.UnitID, request.Address, request.Quantity);
        }

        private int WriteRequestWriteSingleCoil(Header header, RequestWriteSingleCoil request)
        {
            return WriteSetSingleCoilRequest(header.TransactionID, header.UnitID, request.Address, request.CoilState);
        }

        private int WriteHeader(Memory<byte> memory, ushort transactionID, byte unitID, ushort length)
        {
            var written = 0;
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), transactionID);
            written += sizeof(ushort);

            // Write Protocol ID
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), PROTOCOL_ID);
            written += sizeof(ushort);

            // Write length
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), length);
            written += sizeof(ushort);

            // Write Unit ID
            memory.Span[written] = unitID;
            written += sizeof(byte);

            return written;
        }
    }
}
