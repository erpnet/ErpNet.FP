using System;

namespace ErpNet.FP.Print.Drivers
{
    [Serializable]
    public class DeviceReadTimeoutException : Exception
    {
        public DeviceReadTimeoutException() { }
        public DeviceReadTimeoutException(string message) : base(message) { }
        public DeviceReadTimeoutException(string message, Exception inner) : base(message, inner) { }
        protected DeviceReadTimeoutException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
