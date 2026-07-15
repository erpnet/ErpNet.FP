#nullable enable
namespace ErpNet.FP.Core.Drivers.BgSis
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using ErpNet.FP.Core.Configuration;
    using Newtonsoft.Json.Linq;
    using Serilog;

    public class BgSisJsonFiscalPrinterDriver : FiscalPrinterDriver
    {
        public override string DriverName => "bg.sis.json";

        private static readonly Regex _fiscalDeviceNumberPattern =
            new Regex("^[A-Z]{2}[0-9]{6}$", RegexOptions.Compiled);

        public override IFiscalPrinter Connect(
            IChannel channel,
            ServiceOptions serviceOptions,
            bool autoDetect = true,
            IDictionary<string, string>? options = null)
        {
            var fiscalPrinter = new BgSisJsonFiscalPrinter(channel, serviceOptions, options);
            var rawDeviceInfoCacheKey = $"sis.{channel.Descriptor}.{DriverName}";
            lock (channel)
            {
                var rawDeviceInfo = Cache.Get(rawDeviceInfoCacheKey);
                if (rawDeviceInfo == null)
                {
                    (rawDeviceInfo, _) = fiscalPrinter.GetRawDeviceInfo();
                    Log.Information($"RawDeviceInfo({channel.Descriptor}): {rawDeviceInfo}");
                    Cache.Store(rawDeviceInfoCacheKey, rawDeviceInfo, TimeSpan.FromSeconds(30));
                }

                var (model, fwChecksum) = fiscalPrinter.GetModelAndChecksum();
                fiscalPrinter.Info = ParseDeviceInfo(rawDeviceInfo, model, fwChecksum, autoDetect);
                fiscalPrinter.Info.SupportedPaymentTypes = fiscalPrinter.GetSupportedPaymentTypes();
                // Subtotal (receipt-level) amount modifiers are supported, but the SIS module requires a
                // VAT category (enumVatCategory) on each subtotal entry. The driver maps it from the
                // item's optional taxGroup (pure translation - it never infers/allocates the VAT itself);
                // the extra flag tells callers that taxGroup is mandatory here for such items.
                fiscalPrinter.Info.SupportsSubTotalAmountModifiers = true;
                fiscalPrinter.Info.SubTotalAmountModifiersRequireTaxGroup = true;
                // The SIS module prints extended fiscal receipts (invoice / Credit Memo). The number
                // (invNumber) must always be supplied by the caller (>= 1); the device never assigns it.
                fiscalPrinter.Info.SupportsInvoice = true;
                fiscalPrinter.Info.SupportsCreditNote = true;
                fiscalPrinter.Info.InvoiceNumberAssignment = NumberAssignment.ExternalRequired;
                fiscalPrinter.Info.CreditNoteNumberAssignment = NumberAssignment.ExternalRequired;
                serviceOptions.ReconfigurePrinterConstants(fiscalPrinter.Info);

                return fiscalPrinter;
            }
        }

        protected DeviceInfo ParseDeviceInfo(string rawDeviceInfo, string model, string fwChecksum, bool autoDetect)
        {
            JObject json;
            try
            {
                json = JObject.Parse(rawDeviceInfo);
            }
            catch (Exception ex)
            {
                throw new InvalidDeviceInfoException(
                    $"getMfcInfo did not return valid JSON for '{DriverName}': {ex.Message}");
            }

            var fdNumber = (json.Value<string>("FDNumber") ?? string.Empty).Trim();
            var fmNumber = (json.Value<string>("FMNumber") ?? string.Empty).Trim();
            var idNumber = (json.Value<string>("IDNumber") ?? string.Empty).Trim();

            if (autoDetect)
            {
                if (!_fiscalDeviceNumberPattern.IsMatch(fdNumber))
                {
                    throw new InvalidDeviceInfoException(
                        $"FDNumber '{fdNumber}' is not in the expected format (2 letters + 6 digits) for '{DriverName}'");
                }

                if (string.IsNullOrEmpty(model))
                {
                    throw new InvalidDeviceInfoException($"printerModel is empty for '{DriverName}'");
                }
            }

            var (commentMax, itemMax) = GetTextLimitsForModel(model);
            return new DeviceInfo
            {
                SerialNumber = fdNumber,
                FiscalMemorySerialNumber = fmNumber,
                Model = string.IsNullOrEmpty(model) ? "SIS Fiscal Module" : model,
                FirmwareVersion = fwChecksum,
                Manufacturer = "SIS Technology",
                TaxIdentificationNumber = idNumber,
                CommentTextMaxLength = commentMax,
                ItemTextMaxLength = itemMax,
                OperatorPasswordMaxLength = 8
            };
        }

        /// <summary>
        /// Printable text limits per model. commentMax is the per-line character limit for free text
        /// (verified on MF-P1200DN: 42 chars per line); itemMax is the item description limit (64).
        /// Note: the SIS spec's "31" for text lines is NOT a length limit - it is a separate rule that
        /// DIGITS are only allowed in the first 31 characters of a line. That rule is a device quirk that
        /// cannot be expressed in DeviceInfo and is left to the caller's formatting.
        /// </summary>
        protected static (int commentMax, int itemMax) GetTextLimitsForModel(string model)
        {
            return model switch
            {
                "MF-P1200DN" => (42, 64),
                "MF-TH250QR" => (42, 64),
                "MF-TH230QR" => (42, 64),
                "BULPRINT T2QR" => (46, 64),
                "BULPRINT T3QR" => (46, 64),
                _ => (42, 64)
            };
        }
    }
}
