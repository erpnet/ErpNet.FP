namespace ErpNet.FP.Core
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Describes how the number of an invoice or credit note is assigned for a device.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NumberAssignment
    {
        /// <summary>
        /// The device assigns the number itself.
        /// </summary>
        [EnumMember(Value = "device-assigned")]
        DeviceAssigned = 0,

        /// <summary>
        /// The caller may supply the number; when omitted, the device assigns it.
        /// </summary>
        [EnumMember(Value = "external-optional")]
        ExternalOptional = 1,

        /// <summary>
        /// The caller must supply the number.
        /// </summary>
        [EnumMember(Value = "external-required")]
        ExternalRequired = 2
    }
}
