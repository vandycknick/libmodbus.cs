namespace LibModbus.Frame
{
    internal enum ModbusFunction : byte
    {
        ReadCoils = 0x01,
        ReadDiscreteInputs = 0x02,
        WriteSingleCoil = 0x05,
        WriteMultipleCoils = 0x0F,
    }
}
