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

        private int WriteRequestReadCoils(Header header, RequestReadCoils request)
        {
            var memory = _writer.GetMemory(HEADER_LEN + sizeof(ushort) * 2 + sizeof(byte));

            var written = WriteHeader(memory, header.TransactionID, header.UnitID, 6);

            // Write Function Code
            memory.Span[written] = (byte)ModbusFunction.ReadCoilStatus;
            written += sizeof(byte);

            // Write Address Data
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), request.Address);
            written += sizeof(ushort);

            // Write Quantity Data
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), request.Quantity);
            written += sizeof(ushort);

            return written;
        }

        private int WriteRequestWriteSingleCoil(Header header, RequestWriteSingleCoil request)
        {
            var memory = _writer.GetMemory(HEADER_LEN + sizeof(ushort) * 2 + sizeof(byte));

            var written = WriteHeader(memory, header.TransactionID, header.UnitID, 6);

            // Write Function Code
            memory.Span[written] = (byte)ModbusFunction.WriteSingleCoil;
            written += sizeof(byte);

            // Write Address Data
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), request.Address);
            written += sizeof(ushort);

            // Write State
            var data = ModbusFrameUtils.BoolToCoil(request.CoilState);
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), data);
            written += sizeof(ushort);

            return written;
        }

        private int WriteRequestWriteMultipleCoils(Header header, RequestWriteMultipleCoils request)
        {
            var memory = _writer.GetMemory(HEADER_LEN + sizeof(ushort) * 2 + sizeof(byte));
            var byteCount = (byte)((request.CoilStates.Length % 8 != 0 ? request.CoilStates.Length / 8 + 1 : (request.CoilStates.Length / 8)));
            var length = (ushort)(HEADER_LEN + byteCount);

            var written = WriteHeader(memory, header.TransactionID, header.UnitID, length);

            // Write Function Code
            memory.Span[written] = (byte)ModbusFunction.WriteMultipleCoils;
            written += sizeof(byte);

            // Write Address Data
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), request.Address);
            written += sizeof(ushort);

            // Write Number of registers
            BinaryPrimitives.WriteUInt16BigEndian(memory.Span.Slice(written, 2), (ushort)request.CoilStates.Length);
            written += sizeof(ushort);

            // Write byte count
            memory.Span[written] = byteCount;
            written += sizeof(byte);

            // Write bytes
            byte singleCoilValue = 0;
            for (int i = 0; i < request.CoilStates.Length; i++)
            {
                if ((i % 8) == 0) singleCoilValue = 0;

                var coilValue = request.CoilStates[i] == true ? (byte)1 : (byte)0;

                singleCoilValue = (byte)(coilValue << (i % 8) | singleCoilValue);

                memory.Span[written + (i / 8)] = singleCoilValue;
            }
            written = written + byteCount;

            return written;
        }
    }
}
