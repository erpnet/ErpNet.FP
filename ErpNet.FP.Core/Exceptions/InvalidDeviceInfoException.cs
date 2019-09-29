namespace ErpNet.FP.Core.Drivers
{
    using System;

    [Serializable]
    public class InvalidDeviceInfoException : Exception
    {
        public InvalidDeviceInfoException() { }
        public InvalidDeviceInfoException(string message) : base(message) { }
        public InvalidDeviceInfoException(string message, Exception inner) : base(message, inner) { }
        protected InvalidDeviceInfoException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
