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
    }
}
