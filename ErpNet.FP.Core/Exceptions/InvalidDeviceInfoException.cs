namespace ErpNet.FP.Core.Drivers
{
    using System;

    public class InvalidDeviceInfoException : Exception
    {
        public InvalidDeviceInfoException() { }

        public InvalidDeviceInfoException(string message) : base(message) { }

        public InvalidDeviceInfoException(string message, Exception inner) : base(message, inner) { }
    }
}
