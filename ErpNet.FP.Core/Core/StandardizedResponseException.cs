using System;

namespace ErpNet.FP.Core
{
    [Serializable]
    public class StandardizedResponseException : Exception
    {
        public string Code = string.Empty;
        public StatusMessageType Type = StatusMessageType.Error;
        public StandardizedResponseException() { }
        public StandardizedResponseException(string message) : base(message) { }

        public StandardizedResponseException(string message, string code) : base(message)
        {
            Code = code;
        }
        public StandardizedResponseException(string message, string code, StatusMessageType type) : base(message)
        {
            Code = code;
            Type = type;
        }
        public StandardizedResponseException(string message, Exception inner) : base(message, inner) { }
        protected StandardizedResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
