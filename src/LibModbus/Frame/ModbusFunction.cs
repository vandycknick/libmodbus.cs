namespace LibModbus.Frame
{
    internal enum ModbusFunction : byte
    {
        ReadCoils = 0x01,
        ReadDiscreteInputs = 0x02,
        ReadHoldingRegisters = 0x03,
        ReadInputRegisters = 0x04,
        WriteSingleCoil = 0x05,
        WriteMultipleCoils = 0x0F,
    }
}
