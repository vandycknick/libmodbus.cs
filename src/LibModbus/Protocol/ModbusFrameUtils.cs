using System;

namespace LibModbus.Protocol
{
    internal class ModbusFrameUtils
    {
        public static bool CoilToBool(ushort coil) => coil switch
        {
            0xFF00 => true,
            0x0000 => false,
            _ => throw new Exception($"Invalid coil value {coil}"),
        };

        public static ushort BoolToCoil(bool state) => state switch
        {
            true => 0xFF00,
            false => 0x0000,
        };

        public static int GetByteCount(bool[] states) =>
            (states.Length % 8 != 0 ? states.Length / 8 + 1 : (states.Length / 8));

        public static int PackCoils(Span<byte> buffer, bool[] states)
        {
            var byteCount = GetByteCount(states);
            byte singleCoilValue = 0;
            var i = 0;

            for (; i < states.Length; i++)
            {
                if ((i % 8) == 0) singleCoilValue = 0;

                var coilValue = states[i] == true ? (byte)1 : (byte)0;

                singleCoilValue = (byte)(coilValue << (i % 8) | singleCoilValue);

                buffer[(i / 8)] = singleCoilValue;
            }

            return byteCount;
        }
    }
}
