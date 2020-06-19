namespace LibModbus.Frame
{
    internal readonly struct Header
    {
        public static Header Empty = new Header(0, 0);
        public Header(ushort transactionID, byte unitID)
        {
            TransactionID = transactionID;
            UnitID = unitID;    
        }

        public ushort TransactionID { get; }
        public byte UnitID { get; }
    }
}
