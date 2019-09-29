namespace ErpNet.FP.Core.Drivers
{
    using System;

    [Serializable]
    public class InvalidResponseException : Exception
    {
        public InvalidResponseException() { }
        public InvalidResponseException(string message) : base(message) { }
        public InvalidResponseException(string message, Exception inner) : base(message, inner) { }
        protected InvalidResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
