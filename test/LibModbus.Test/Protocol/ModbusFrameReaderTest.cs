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
        public void ModbusFrameReader_ReadFrame_ParsesAWriteSingleCoilResponseTurnedOn()
        {
            //Given
            var header = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x09, };
            var data = new byte[] { 0x05, 0x00, 0x04, 0xFF, 0x00 };

            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, 5);

            //When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            //Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(9, frame.Header.UnitID);

            var response = Assert.IsType<ResponseWriteSingleCoil>(frame.Pdu);
            Assert.True(response.Result);

            Assert.Equal(sequence.End, position);
        }

        [Fact]
        public void ModbusFrameReader_ReadFrame_ParsesAWriteSingleCoilResponseTurnedOff()
        {
            //Given
            var header = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x09, };
            var data = new byte[] { 0x05, 0x00, 0x04, 0x00, 0x00 };


            var first = new MemorySegment<byte>(header);
            var last = first.Append(data);
            var sequence = new ReadOnlySequence<byte>(first, 0, last, 5);

            //When
            var reader = new ModbusFrameReader(sequence);
            var position = reader.ReadFrame(out var frame);

            //Then
            Assert.Equal(1, frame.Header.TransactionID);
            Assert.Equal(9, frame.Header.UnitID);

            var response = Assert.IsType<ResponseWriteSingleCoil>(frame.Pdu);
            Assert.False(response.Result);

            Assert.Equal(sequence.End, position);
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
