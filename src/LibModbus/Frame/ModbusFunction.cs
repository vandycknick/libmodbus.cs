namespace LibModbus.Frame
{
    internal enum ModbusFunction : byte
    {
        ReadCoilStatus = 0x01,
        WriteSingleCoil = 0x05,
        WriteMultipleCoils = 0x0F,
    }
}
