using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ErpNet.FP.Core
{
    public enum StatusMessageType
    {
        [EnumMember(Value = "")]
        Unknown,
        [EnumMember(Value = "reserved")]
        Reserved,
        [EnumMember(Value = "info")]
        Info,
        [EnumMember(Value = "warning")]
        Warning,
        [EnumMember(Value = "error")]
        Error
    }

    public class StatusMessage
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public StatusMessageType Type { get; set; } = StatusMessageType.Unknown;

        /* Error and Warning Codes are strings with length of 5 characters.
        First 3 characters are the type of the error, i.e., ERR, WRN.
        Next 2 characters are digits, representing the ID of the error or warning. */
        public string Code { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class DeviceStatus
    {
        private int ErrorsCount = 0;
        public IList<StatusMessage> Messages { get; protected set; } = new List<StatusMessage>();

        public bool Ok => ErrorsCount == 0;

        public void AddMessage(StatusMessage statusMessage)
        {
            if (statusMessage.Type == StatusMessageType.Unknown)
            {
                throw new System.ArgumentException("status message type cannot be unknown");
            }
            if (statusMessage.Type == StatusMessageType.Reserved)
            {
                // Ignore reserved messages
                return;
            }
            if (statusMessage.Type == StatusMessageType.Error)
            {
                ErrorsCount++;
            }
            Messages.Add(statusMessage);
        }

        public void AddInfo(string text)
        {
            AddMessage(new StatusMessage
            {
                Type = StatusMessageType.Info,
                Text = text
            });
        }

        public void AddError(string code, string text)
        {
            AddMessage(new StatusMessage
            {
                Type = StatusMessageType.Error,
                Code = code,
                Text = text
            });
        }

        public void AddWarning(string code, string text)
        {
            AddMessage(new StatusMessage
            {
                Type = StatusMessageType.Warning,
                Code = code,
                Text = text
            });
        }
    }

    public class DeviceStatusEx : DeviceStatus
    {
        public System.DateTime DeviceDateTime { get; set; }

        public DeviceStatusEx(DeviceStatus status) : base()
        {
            Messages = status.Messages;
        }
    }

}
