namespace LibModbus.Frame
{
    internal struct ResponseAdu
    {
        public static ResponseAdu Empty = new ResponseAdu { Header = Header.Empty, Pdu = null };
        public Header Header { get; set; }
        public IResponse Pdu { get; set; }
    }

    internal interface IResponse
    {

    }

    internal struct ResponseReadCoils : IResponse
    {
        public byte[] Coils { get; set; }
    }

    internal struct ResponseWriteSingleCoil : IResponse
    {
        public bool Result { get; set; }
    }

    internal struct ResponseError : IResponse
    {
        public ModbusErrorCode ErrorCode { get; set; }
    }
}
