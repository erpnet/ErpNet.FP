namespace ErpNet.FP.Core
{
    using System;

    [Serializable]
    public class StandardizedStatusMessageException : Exception
    {
        public string Code = string.Empty;
        public StatusMessageType Type = StatusMessageType.Error;
        public StandardizedStatusMessageException() { }
        public StandardizedStatusMessageException(string message) : base(message) { }

        public StandardizedStatusMessageException(string message, string code) : base(message)
        {
            Code = code;
        }
        public StandardizedStatusMessageException(string message, string code, StatusMessageType type) : base(message)
        {
            Code = code;
            Type = type;
        }
        public StandardizedStatusMessageException(string message, Exception inner) : base(message, inner) { }
        protected StandardizedStatusMessageException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
