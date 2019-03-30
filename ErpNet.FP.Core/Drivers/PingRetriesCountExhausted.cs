using System;

namespace ErpNet.FP.Core.Drivers
{
    [Serializable]
    public class PingRetriesCountExhausted : Exception
    {
        public PingRetriesCountExhausted() { }
        public PingRetriesCountExhausted(string message) : base(message) { }
        public PingRetriesCountExhausted(string message, Exception inner) : base(message, inner) { }
        protected PingRetriesCountExhausted(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
