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
        public bool Ok { get; private set; } = true;
        public IList<StatusMessage> Messages { get; protected set; } = new List<StatusMessage>();

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
                Ok = false;
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

    public class DeviceStatusWithDateTime : DeviceStatus
    {
        public System.DateTime DeviceDateTime { get; set; }

        public DeviceStatusWithDateTime(DeviceStatus status) : base()
        {
            Messages = status.Messages;
        }
    }

    public class DeviceStatusWithReceiptInfo : DeviceStatus
    {
        /// <summary>
        /// The receipt number.
        /// </summary>
        public string ReceiptNumber = string.Empty;
        /// <summary>
        /// The receipt date and time.
        /// </summary>
        public System.DateTime ReceiptDateTime;
        /// <summary>
        /// The receipt amount.
        /// </summary>
        public decimal ReceiptAmount = 0m;
        /// <summary>
        /// The fiscal memory number.
        /// </summary>
        public string FiscalMemorySerialNumber = string.Empty;

        public DeviceStatusWithReceiptInfo(DeviceStatus status, ReceiptInfo info) : base()
        {
            Messages = status.Messages;
            ReceiptNumber = info.ReceiptNumber;
            ReceiptDateTime = info.ReceiptDateTime;
            ReceiptAmount = info.ReceiptAmount;
            FiscalMemorySerialNumber = info.FiscalMemorySerialNumber;
        }
    }

}
