using LibModbus.Frame;
using LibModbus.Protocol;
using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace LibModbus.Test.Protocol
{
    public class ModbusFrameWriterTest
    {
        [Fact]
        public void ModbusFrameWriter_WriteFrame_CorrectlyWritesAReadCoilsRequest()
        {
            // Given
            var request = new RequestAdu
            {
                Header = new Header(transactionID: 1, unitID: 4),
                Pdu = new RequestReadCoils
                {
                    Address = 0x0040,
                    Quantity = 0x000A,
                },
            };
            var arraybuffer = new ArrayBufferWriter<byte>();

            // When
            var writer = new ModbusFrameWriter(arraybuffer);
            var position = writer.WriteFrame(request);
            arraybuffer.Advance(position);

            // Then
            var data = arraybuffer.WrittenSpan.ToArray();
            Assert.Equal(
                new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x04, 0x01, 0x00, 0x40, 0x00, 0x0A },
                data
            );
        }

        [Fact]
        public void ModbusFrameWriter_WriteFrame_CorrectlyWritesAReadDiscreteInputsRequet()
        {
            // Given
            var request = new RequestAdu
            {
                Header = new Header(transactionID: 1, unitID: 4),
                Pdu = new RequestReadDiscreteInputs
                {
                    Address = 0x0040,
                    Quantity = 0x000A,
                },
            };
            var arraybuffer = new ArrayBufferWriter<byte>();

            // When
            var writer = new ModbusFrameWriter(arraybuffer);
            var position = writer.WriteFrame(request);
            arraybuffer.Advance(position);

            // Then
            var data = arraybuffer.WrittenSpan.ToArray();
            Assert.Equal(
                new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x04, 0x02, 0x00, 0x40, 0x00, 0x0A },
                data
            );
        }

        [Fact]
        public void ModbusFrameWriter_WriteFrame_CorrectlyWritesAWriteSingleCoilRequest()
        {
            // Given
            var request = new RequestAdu
            {
                Header = new Header(transactionID: 1, unitID: 4),
                Pdu = new RequestWriteSingleCoil
                {
                    Address = 0x0041,
                    CoilState = true,
                },
            };
            var arraybuffer = new ArrayBufferWriter<byte>();

            // When
            var writer = new ModbusFrameWriter(arraybuffer);
            var position = writer.WriteFrame(request);
            arraybuffer.Advance(position);

            // Then
            var data = arraybuffer.WrittenSpan.ToArray();
            Assert.Equal(
                new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x04, 0x05, 0x00, 0x41, 0xFF, 0x00 },
                data
            );
        }

        [Theory]
        [MemberData(nameof(GetWriteMultipleCoilsRequestTestData))]
        public void ModbusFrameWriter_WriteFrame_CorrectlyWritesAWriteMultipleCoilsRequest(object request, byte[] expected)
        {
            // Given
            var arraybuffer = new ArrayBufferWriter<byte>();

            // When
            var writer = new ModbusFrameWriter(arraybuffer);
            var position = writer.WriteFrame((RequestAdu)request);
            arraybuffer.Advance(position);

            // Then
            var data = arraybuffer.WrittenSpan.ToArray();
            Assert.Equal(expected, data);
        }

        public static IEnumerable<object[]> GetWriteMultipleCoilsRequestTestData()
        {

            yield return new object[]
            {
                new RequestAdu
                {
                    Header = new Header(transactionID: 1, unitID: 4),
                    Pdu = new RequestWriteMultipleCoils
                    {
                        Address = 0x42,
                        CoilStates = new bool[] { true, false, true, true , false, true, false, false }
                    },
                }, // Request
                new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x08, 0x04, 0x0F, 0x00, 0x42, 0x00, 0x08, 0x01, 0x2D, }, // Result
            };

            yield return new object[]
            {
                new RequestAdu
                {
                    Header = new Header(transactionID: 2, unitID: 4),
                    Pdu = new RequestWriteMultipleCoils
                    {
                        Address = 0x42,
                        CoilStates = new bool[] { true, false, true, }
                    },
                }, // Request
                new byte[] { 0x00, 0x02, 0x00, 0x00, 0x00, 0x08, 0x04, 0x0F, 0x00, 0x42, 0x00, 0x03, 0x01, 0x05, }, // Result
            };

            yield return new object[]
            {
                new RequestAdu
                {
                    Header = new Header(transactionID: 3, unitID: 4),
                    Pdu = new RequestWriteMultipleCoils
                    {
                        Address = 0x42,
                        CoilStates = new bool[] { true, false, true, true, true, false, false, true, true, false, true }
                    },
                }, // Request
                new byte[] { 0x00, 0x03, 0x00, 0x00, 0x00, 0x09, 0x04, 0x0F, 0x00, 0x42, 0x00, 0x0B, 0x02,0x9D, 0x05, }, // Result
            };
        }
    }
}
