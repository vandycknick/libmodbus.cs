namespace LibModbus.Frame
{
    internal struct RequestAdu
    {
        public Header Header { get; set; }
        public IRequestPdu Pdu { get; set; }
    }

    internal interface IRequestPdu
    {

    }

    internal struct RequestReadCoils : IRequestPdu
    {
        public ushort Address { get; set; }
        public ushort Quantity { get; set; }
    }

    internal struct RequestWriteSingleCoil : IRequestPdu
    {
        public ushort Address { get; set; }
        public bool CoilState { get; set; }
    }

    internal struct RequestWriteMultipleCoils : IRequestPdu
    {
        public ushort Address { get; set; }
        public bool[] CoilStates { get; set; }
    }
}
