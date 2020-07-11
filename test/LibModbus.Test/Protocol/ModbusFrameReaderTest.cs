using System;
using System.Buffers;
using LibModbus.Protocol;
using LibModbus.Frame;
using Xunit;

namespace LibModbus.Test.Protocol
{
    public class ModbusFrameReaderTest
    {
        [Fact]
        public void ModbusFrameReader_ReadFrame_ParsesAReadCoilResponse()
        {
            // Given
            var header = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x09, };
            var data = new byte[] { 0x01, 0x01, 0x01, };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, 3);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(9, frame.Header.UnitID);

            var read = Assert.IsType<ResponseReadCoils>(frame.Pdu);
            Assert.Equal(new byte[] { 1 }, read.Coils);

            Assert.Equal(sequence.End, position);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_ParsesAReadDiscreteInputsResponse()
        {
            // Given
            var header = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x09, };
            var data = new byte[] { 0x02, 0x01, 0x01, };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, 3);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(9, frame.Header.UnitID);

            var read = Assert.IsType<ResponseReadDiscreteInputs>(frame.Pdu);
            Assert.Equal(new byte[] { 1 }, read.Coils);

            Assert.Equal(sequence.End, position);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_ParsesAReadHoldingRegistersResponse()
        {
            // Given
            var header = new byte[]
            {
                0x00, 0x1E, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x07, // Message length
                0x18,       // Device id / Unit id
            };
            var data = new byte[]
            {
                0x03,       // Function code
                0x04,       // Number of bytes more
                0xAA, 0x00, // Register value Hi and Li (AO0)
                0x11, 0x11, // Register value Hi and Li (AO0)
            };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, data.Length);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(30, frame.Header.TransactionID);
            Assert.Equal(24, frame.Header.UnitID);

            var response = Assert.IsType<ResponseReadHoldingRegisters>(frame.Pdu);
            Assert.Equal(
                new ushort[] { 43520, 4369 },
                response.Results
            );

            Assert.Equal(sequence.End, position);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_ParsesAReadInputRegistersResponse()
        {
            // Given
            var header = new byte[]
            {
                0x00, 0xCE, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x09, // Message length
                0x12,       // Device id / Unit id
            };
            var data = new byte[]
            {
                0x04,       // Function code
                0x06,       // Number of bytes more
                0xAA, 0x00, // Register value Hi and Li (AI0)
                0xCC, 0xBB, // Register value Hi and Li (AI1)
                0xEE, 0xDD, // Register value Hi and Li (AI2)
            };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, data.Length);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(206, frame.Header.TransactionID);
            Assert.Equal(18, frame.Header.UnitID);

            var response = Assert.IsType<ResponseReadInputRegisters>(frame.Pdu);
            Assert.Equal(
                new ushort[] { 43520, 52411, 61149, },
                response.Results
            );

            Assert.Equal(sequence.End, position);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_ParsesAWriteSingleCoilResponseTurnedOn()
        {
            // Given
            var header = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Message length
                0x09,       // Device id / Unit id
            };
            var data = new byte[]
            {
                0x05,       // Function code
                0x00,       // Hi Register Address byte
                0x04,       // Lo Register Address byte
                0xFF,       // Hi Byte Meaning
                0x00,       // Lo Byte Meaning
            };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, 5);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(9, frame.Header.UnitID);

            var response = Assert.IsType<ResponseWriteSingleCoil>(frame.Pdu);
            Assert.True(response.Result);

            Assert.Equal(sequence.End, position);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_ParsesAWriteSingleCoilResponseTurnedOff()
        {
            // Given
            var header = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Message length
                0x09,       // Device id / Unit id
            };
            var data = new byte[]
            {
                0x05,       // Function code
                0x00,       // Hi Register Address byte
                0x04,       // Lo Register Address byte
                0x00,       // Hi Byte Meaning
                0x00,       // Lo Byte Meaning
            };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, 5);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(9, frame.Header.UnitID);

            var response = Assert.IsType<ResponseWriteSingleCoil>(frame.Pdu);
            Assert.False(response.Result);

            Assert.Equal(sequence.End, position);
        }


        [Fact]
        public void ModbusFrameReader_ReadFrame_ParsesAWriteMultipleCoilsResponse()
        {
            // Given
            var header = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Message length
                0x20,       // Device id / Unit id
            };
            var data = new byte[]
            {
                0x0F,       // Function code
                0x00,       // Address of the first byte of register Hi
                0x12,       // Address of the first byte of register Lo
                0x00,       // Number of recorded reg. Hi byte
                0x04,       // Number of recorded reg. Lo bytes
            };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, data.Length);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(32, frame.Header.UnitID);

            var response = Assert.IsType<ResponseWriteMultipleCoils>(frame.Pdu);
            Assert.Equal(18, response.Address);
            Assert.Equal(4, response.Quantity);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_CorrectlyReadsAFrameThatsASingleSegment()
        {
            // Given
            var header = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x09, 0x01, 0x01, 0x01, };
            var sequence = new ReadOnlySequence<byte>(header, 0, header.Length);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(9, frame.Header.UnitID);

            var read = Assert.IsType<ResponseReadCoils>(frame.Pdu);
            Assert.Equal(new byte[] { 1 }, read.Coils);
        }


        [Fact]
        public void ModbusFrameReader_ReadFrame_CorrectlyReadsAFrameSplitOverMultipleSegments()
        {
            // Given
            var arrayOne = new byte[] { 0x00, 0x01, };
            var arrayTwo = new byte[] { 0x00, 0x00, };
            var arrayThree = new byte[] { 0x00, 0x04, };
            var arrayFour = new byte[] { 0x09 };
            var arrayFive = new byte[] { 0x01 };
            var arraySix = new byte[] { 0x01, 0x01, };

            var first = new MemorySegment<byte>(arrayOne);
            var last = first.Append(arrayTwo).Append(arrayThree).Append(arrayFour).Append(arrayFive).Append(arraySix);

            var sequence = new ReadOnlySequence<byte>(first, 0, last, 2);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(9, frame.Header.UnitID);

            var read = Assert.IsType<ResponseReadCoils>(frame.Pdu);
            Assert.Equal(new byte[] { 1 }, read.Coils);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_StopsReadingWhenHeaderIsTooShortAndGoesBackToStart()
        {
            //Given
            var data = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x09, 0x01 };
            var sequence = new ReadOnlySequence<byte>(data, 0, data.Length);

            //When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            //Then
            Assert.Equal(ResponseAdu.Empty, frame);
            Assert.Equal(sequence.Start, position);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_StopsReadingWhenBodyIsTooShortAndGoesBackToStart()
        {
            //Given
            var data = new byte[] { 0x00, 0x01, };
            var sequence = new ReadOnlySequence<byte>(data, 0, data.Length);

            //When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            //Then
            Assert.Equal(ResponseAdu.Empty, frame);
            Assert.Equal(sequence.Start, position);
        }

        [Theory]
        [InlineData((byte)ModbusFunction.ReadCoils)]
        [InlineData((byte)ModbusFunction.ReadDiscreteInputs)]
        [InlineData((byte)ModbusFunction.ReadInputRegisters)]
        [InlineData((byte)ModbusFunction.ReadHoldingRegisters)]
        [InlineData((byte)ModbusFunction.WriteSingleCoil)]
        [InlineData((byte)ModbusFunction.WriteMultipleCoils)]
        public void ModbusFrameReader_ReadFrame_ReturnsAnErrorResponseForEachFunctionWhenTheErrorBitIsSet(byte function)
        {
            // Given
            var errorFunction = (byte)(function | 0x80);
            var header = new byte[]
            {
                0x00, 0x01, // TransactionID
                0x00, 0x00, // Protocol ID
                0x00, 0x03, // Message length
                0x20,       // Device address
            };
            var data = new byte[]
            {
                errorFunction,  // Functional code with changed bit
                0x01            // Error Code
            };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, data.Length);

            // When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            // Then
            var read = Assert.IsType<ResponseError>(frame.Pdu);
            Assert.Equal(ModbusErrorCode.UnknownFunction, read.ErrorCode);
        }

        internal class MemorySegment<T> : ReadOnlySequenceSegment<T>
        {
            public MemorySegment(ReadOnlyMemory<T> memory)
            {
                Memory = memory;
            }

            public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
            {
                var segment = new MemorySegment<T>(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };

                Next = segment;

                return segment;
            }
        }
    }
}
