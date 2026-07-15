namespace ErpNet.FP.Core
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// The kind of legal identifier that identifies the recipient (buyer) party.
    /// Country-neutral: each driver maps these to its device-specific codes.
    /// </summary>
    public enum IdentifierType
    {
        /// <summary>
        /// No identifier type specified. Rejected when validating an invoice / credit note, so a
        /// caller that omits the type gets a clear error instead of a silently-assumed value.
        /// </summary>
        [EnumMember(Value = "unspecified")]
        Unspecified = 0,

        /// <summary>
        /// Registration identifier of a domestic (resident) company. Drivers validate it against the
        /// device's domestic scheme (e.g. a Bulgarian EIK / Bulstat), so a foreign number must NOT use
        /// this type - use <see cref="ForeignerId"/> instead.
        /// </summary>
        [EnumMember(Value = "legal-registration")]
        LegalRegistration = 1,

        /// <summary>
        /// National identification number of a domestic (resident) natural person (e.g. a Bulgarian EGN).
        /// </summary>
        [EnumMember(Value = "national-id")]
        NationalId = 2,

        /// <summary>
        /// Identifier of a foreign (non-resident) party - company or natural person. Use for any
        /// non-domestic buyer; the value may be alphanumeric.
        /// </summary>
        [EnumMember(Value = "foreigner-id")]
        ForeignerId = 3,

        /// <summary>
        /// Tax-authority-assigned number.
        /// </summary>
        [EnumMember(Value = "tax-number")]
        TaxNumber = 4
    }

    /// <summary>
    /// Represents the recipient (buyer) party of an invoice or credit note.
    /// Field naming follows EN 16931 buyer terminology; drivers translate to device specifics.
    /// </summary>
    public class Recipient
    {
        /// <summary>
        /// Buyer name (legal/registered name).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Buyer legal registration identifier.
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// The kind of <see cref="Identifier"/>. Must be specified for an invoice / credit note.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public IdentifierType IdentifierType { get; set; } = IdentifierType.Unspecified;

        /// <summary>
        /// Buyer VAT identifier.
        /// </summary>
        public string VatNumber { get; set; } = string.Empty;

        /// <summary>
        /// Buyer postal address.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Buyer city / town. Kept separate from <see cref="Address"/> because some devices require the city as a distinct field.
        /// </summary>
        public string City { get; set; } = string.Empty;
    }
}
