using System;

namespace ErpNet.FP.Core
{
    /// <summary>All the state that is required to connect to a fiscal printer. Should be passed with each operation</summary>
    public class FiscalPrinterState
    {
        public Type Driver;

        /// <summary>Fiscal printer operator. Usually, this is a number in the range [1-4]</summary>
        public string Operator;
        /// <summary>Fiscal printer operator password. For some devices, the default is 1</summary>
        public string OperatorPassword;
        /// <summary>COM port used for serial communication with the fiscal printer</summary>
        public FiscalPrinterPort ComPort;
        /// <summary>Tranfer seed for COM port communication. Typical values are 9600 and 115200</summary>
        public int BaudRate;
        /// <summary>Some printers expose their API via a server that listens on localhost. Use this field to customize the default address</summary>
        public string DriverApiAddress;
    }
}