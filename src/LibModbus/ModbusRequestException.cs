using System;
using LibModbus.Frame;

namespace LibModbus
{
    public class ModbusRequestException : Exception
    {
        internal ModbusRequestException(ModbusErrorCode code) : this(ErrorCodeToMessage(code))
        {
        }

        public ModbusRequestException(string message) : base(message)
        {
        }

        public ModbusRequestException(string message, Exception inner) : base(message, inner)
        {            
        }

        private static string ErrorCodeToMessage(ModbusErrorCode code)
        {
            switch (code)
            {
                case ModbusErrorCode.UnknownFunction:
                    return "The received function code can not be processed.";

                case ModbusErrorCode.InvalidAddress:
                    return "The address specified in the request is not available.";

                case ModbusErrorCode.InvalidQuery:
                    return "The value contained in the query data field is an invalid value.";

                case ModbusErrorCode.UnrecoverableError:
                    return "An unrecoverable error occurred while the worker attempted to perform the requested action.";
                
                case ModbusErrorCode.WorkerTakesLongToProcess:
                    return "The worker has accepted the request and processes it, but it takes a long time. This response prevents the host from generating a timeout error.";

                case ModbusErrorCode.WorkerBusy:
                    return "The worker is busy processing the command. The client must repeat the message later when the worker is freed.";

                case ModbusErrorCode.WorkerCantExecuteFunction:
                    return "The worker can not execute the program function specified in the request. This code is returned for an unsuccessful program request using functions with numbers 13 or 14. The client must request diagnostic information or error information from the worker.";

                case ModbusErrorCode.WorkerParityError:
                    return "The worker detected a parity error when reading the extended memory. The client can repeat the request, but usually in such cases, repairs are required.";

                default:
                    return "Unkown error!";
            }
        }
    }
}
