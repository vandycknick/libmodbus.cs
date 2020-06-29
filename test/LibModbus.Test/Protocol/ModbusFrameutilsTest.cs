using System;
using LibModbus.Protocol;
using Xunit;

namespace LibModbus.Test.Protocol
{
    public class ModbusFrameUtilsTest
    {
        [Fact]
        public void ModbusFrameUtils_CoilToBool_ReturnsTrueWhenGivenACoilValueOf0xFF00()
        {
            // Given
            ushort coil = 0xFF00;

            // When
            var result = ModbusFrameUtils.CoilToBool(coil);

            // Then
            Assert.True(result);
        }

        [Fact]
        public void ModbusFrameUtils_CoilToBool_ReturnsFalseWhenGivenACoilValueOf0x0000()
        {
            // Given
            ushort coil = 0x0000;

            // When
            var result = ModbusFrameUtils.CoilToBool(coil);

            // Then
            Assert.False(result);
        }

        [Fact]
        public void ModbusFrameUtils_CoilToBool_ThrowsWhenGivenAnInvalidValue()
        {
            // Given
            ushort invalid = 0x0012;

            // When
            var ex = Assert.Throws<Exception>(() => ModbusFrameUtils.CoilToBool(invalid));

            // Then
            Assert.Equal("Invalid coil value 18", ex.Message);
        }

        [Fact]
        public void ModbusFrameUtils_BoolToCoil_Returns0xFF00WhenGivenTrue()
        {
            // Given, When
            var result = ModbusFrameUtils.BoolToCoil(true);

            // Then
            Assert.Equal(0xFF00, result);
        }

        
        [Fact]
        public void ModbusFrameUtils_BoolToCoil_Returns0x0000WhenGivenFalse()
        {
            // Given, When
            var result = ModbusFrameUtils.BoolToCoil(false);

            // Then
            Assert.Equal(0x0000, result);
        }
    }
}
