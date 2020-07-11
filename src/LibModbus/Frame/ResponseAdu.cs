namespace LibModbus.Frame
{
    internal struct ResponseAdu
    {
        public static ResponseAdu Empty = new ResponseAdu { Header = Header.Empty, Pdu = null };
        public Header Header { get; set; }
        public IResponsePdu Pdu { get; set; }
    }

    internal interface IResponsePdu
    {

    }

    internal struct ResponseReadCoils : IResponsePdu
    {
        public byte[] Coils { get; set; }
    }

    internal struct ResponseReadDiscreteInputs : IResponsePdu
    {
        public byte[] Coils { get; set; }
    }

    internal struct ResponseReadInputRegisters : IResponsePdu
    {
        public ushort[] Results { get; set; }
    }

    internal struct ResponseReadHoldingRegisters : IResponsePdu
    {
        public ushort[] Results { get; set; }
    }

    internal struct ResponseWriteSingleCoil : IResponsePdu
    {
        public ushort Address { get; set; }
        public bool Result { get; set; }
    }

    internal struct ResponseWriteMultipleCoils : IResponsePdu
    {
        public ushort Address { get; set; }
        public ushort Quantity { get; set; }
    }

    internal struct ResponseError : IResponsePdu
    {
        public ModbusErrorCode ErrorCode { get; set; }
    }
}
