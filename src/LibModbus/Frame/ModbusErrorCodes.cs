namespace LibModbus.Frame
{
    internal enum ModbusErrorCode : byte
    {
        // The received function code can not be processed.
        UnknownFunction = 1,

        // The data address specified in the request is not available.
        InvalidAddress = 2,

        // The value contained in the query data field is an invalid value.
        InvalidQuery = 3,

        // An unrecoverable error occurred while the worker attempted to perform the requested action.
        UnrecoverableError = 4,
        
        // The worker has accepted the request and processes it, but it takes a long time. This response prevents the host from generating a timeout error.
        WorkerTakesLongToProcess = 5,

        // The worker is busy processing the command. The client must repeat the message later when the worker is freed.
        WorkerBusy = 6,

        // The worker can not execute the program function specified in the request. This code is returned for an unsuccessful program request using functions with numbers 13 or 14. The client must request diagnostic information or error information from the worker.
        WorkerCantExecuteFunction = 7,

        // The worker detected a parity error when reading the extended memory. The client can repeat the request, but usually in such cases, repairs are required.
        WorkerParityError = 8,
    }
}
