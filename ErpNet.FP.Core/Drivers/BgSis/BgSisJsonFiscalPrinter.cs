#nullable enable
namespace ErpNet.FP.Core.Drivers.BgSis
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using ErpNet.FP.Core.Configuration;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Fiscal printer implementation for the SIS Fiscal Module (JSON-RPC over HTTP).
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Drivers.BgFiscalPrinter" />
    public partial class BgSisJsonFiscalPrinter : BgFiscalPrinter
    {
        public BgSisJsonFiscalPrinter(
            IChannel channel,
            ServiceOptions serviceOptions,
            IDictionary<string, string>? options = null)
            : base(channel, serviceOptions, options) { }

        /// <summary>
        /// The optional POS number. It is only emitted when explicitly configured via the "PosId"
        /// option; the device accepts printReceipt without it. When present, it must be at most 6
        /// symbols - the MF-P1200DN rejects a longer posId with EM_PARA_WRONG_FORMAT (0x5D).
        /// </summary>
        protected string? GetPosId()
        {
            // Preferred: per-printer service config (appsettings PrintersProperties.<serial>.PrinterOptions.posId).
            // Fallback: options passed programmatically to the driver.
            var posId = ServiceOptions.GetPrinterOption(Info.SerialNumber, "posId");
            if (string.IsNullOrEmpty(posId) && Options.TryGetValue("PosId", out var value))
            {
                posId = value;
            }

            if (string.IsNullOrEmpty(posId))
            {
                return null;
            }

            return posId.Length > 6 ? posId.Substring(0, 6) : posId;
        }

        public override string GetTaxGroupText(TaxGroup taxGroup)
        {
            if (taxGroup == TaxGroup.Unspecified)
            {
                throw new StandardizedStatusMessageException($"Tax group {taxGroup} unsupported", "E411");
            }

            // SIS enumVatCategory is 0-based: A=0, B=1 ... H=7; TaxGroup is 1-based.
            return ((int)taxGroup - 1).ToString(CultureInfo.InvariantCulture);
        }

        public override IDictionary<PaymentType, string> GetPaymentTypeMappings()
        {
            var paymentTypeMappings = new Dictionary<PaymentType, string>
            {
                { PaymentType.Cash,          "0" },  // Cash
                { PaymentType.Bank,          "1" },  // Bank transfer
                { PaymentType.Card,          "2" },  // Credit/debit card
                { PaymentType.Check,         "3" },  // Cheque
                { PaymentType.InternalUsage, "4" },  // Internal usage
                { PaymentType.Coupons,       "5" },  // Voucher
                { PaymentType.ExtCoupons,    "6" },  // External voucher
                { PaymentType.Reserved1,     "7" },  // NZOK
                { PaymentType.Packaging,     "8" }   // Empties (packaging)
            };

            ServiceOptions.RemapPaymentTypes(Info.SerialNumber, paymentTypeMappings);

            return paymentTypeMappings;
        }

        public override DeviceStatusWithDateTime CheckStatus()
        {
            var (json, status) = Request("getStatus");
            var statusEx = new DeviceStatusWithDateTime(status);
            // "dd-MM-yyyy HH:mm:ss"
            var timestamp = json.Value<string>("timestamp");
            if (!string.IsNullOrEmpty(timestamp)
                && DateTime.TryParseExact(timestamp, "dd-MM-yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                statusEx.DeviceDateTime = dt;
            }

            return statusEx;
        }

        public override DeviceStatus SetDateTime(CurrentDateTime currentDateTime)
        {
            var time = currentDateTime.DeviceDateTime.ToString("HH:mm:ss;dd/MM/yy", CultureInfo.InvariantCulture);
            var (_, status) = Request("setTime", null, new JObject { ["time"] = time });
            return status;
        }

        public override DeviceStatusWithCashAmount Cash(Credentials credentials)
        {
            var (json, status) = Request("getCashBalance");
            var statusEx = new DeviceStatusWithCashAmount(status);
            var cashBalance = json.Value<string>("cashBalance");
            if (!string.IsNullOrEmpty(cashBalance)
                && decimal.TryParse(cashBalance, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                statusEx.Amount = amount;
            }

            return statusEx;
        }

        public override DeviceStatus PrintMoneyDeposit(TransferAmount transferAmount)
        {
            return CashHandling(transferAmount.Amount, transferAmount.Operator);
        }

        public override DeviceStatus PrintMoneyWithdraw(TransferAmount transferAmount)
        {
            if (transferAmount.Amount < 0m)
            {
                throw new StandardizedStatusMessageException("Withdraw amount must be positive number", "E403");
            }

            return CashHandling(-transferAmount.Amount, transferAmount.Operator);
        }

        protected DeviceStatus CashHandling(decimal amount, string @operator)
        {
            var begin = new JObject
            {
                ["operatorNumber"] = int.TryParse(@operator, out var operatorNumber) ? operatorNumber : 1
            };

            var posId = GetPosId();
            if (!string.IsNullOrEmpty(posId))
            {
                begin["posId"] = posId;
            }

            var prms = new JObject
            {
                ["beginFiscalReceiptInput"] = begin,
                ["amount"] = amount.ToString(CultureInfo.InvariantCulture)
            };

            var (_, status) = Request("cashHandling", prms);

            return status;
        }

        public override (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
            return PrintFiscalReceipt(receipt, null);
        }

        public override (ReceiptInfo, DeviceStatus) PrintReversalReceipt(ReversalReceipt reversalReceipt)
        {
            return PrintFiscalReceipt(reversalReceipt, reversalReceipt);
        }

        public override DeviceStatus ValidateInvoice(Invoice invoice) => ValidateInvoiceCore(invoice);

        public override DeviceStatus ValidateCreditNote(CreditNote creditNote) => ValidateCreditNoteCore(creditNote);

        public override (ReceiptInfo, DeviceStatus) PrintInvoice(Invoice invoice)
        {
            JObject prms;
            try
            {
                prms = BuildReceiptParams(invoice, null);
                prms["invoiceData"] = BuildInvoiceData(invoice);
            }
            catch (StandardizedStatusMessageException e)
            {
                return ErrorResult(e);
            }

            return ExecutePrint(prms, invoice, invoice.Number);
        }

        public override (ReceiptInfo, DeviceStatus) PrintCreditNote(CreditNote creditNote)
        {
            JObject prms;
            try
            {
                prms = BuildReceiptParams(creditNote, creditNote);
                var reversal = (JObject)prms["stornoInput"]!;
                if (string.IsNullOrEmpty(creditNote.OriginalInvoiceNumber))
                {
                    throw new StandardizedStatusMessageException(
                        "OriginalInvoiceNumber of the credit note is empty", "E405");
                }

                if (string.IsNullOrEmpty(creditNote.FiscalDeviceSerialNumber))
                {
                    throw new StandardizedStatusMessageException(
                        "FiscalDeviceSerialNumber of the original invoice is required by this device", "E405");
                }

                reversal["invoiceNumber"] = creditNote.OriginalInvoiceNumber;
                reversal["fiscDevNumber"] = creditNote.FiscalDeviceSerialNumber;
                prms["invoiceData"] = BuildInvoiceData(creditNote);
            }
            catch (StandardizedStatusMessageException e)
            {
                return ErrorResult(e);
            }

            return ExecutePrint(prms, creditNote, creditNote.Number);
        }

        protected (ReceiptInfo, DeviceStatus) PrintFiscalReceipt(Receipt receipt, ReversalReceipt? reversal)
        {
            JObject prms;
            try
            {
                prms = BuildReceiptParams(receipt, reversal);
            }
            catch (StandardizedStatusMessageException e)
            {
                return ErrorResult(e);
            }

            return ExecutePrint(prms, receipt, null);
        }

        /// <summary>
        /// Sends the built printReceipt request and resolves the returned receipt info. When
        /// <paramref name="invoiceNumber"/> is non-null the result is an <see cref="InvoiceInfo"/>
        /// carrying that number (invoice / credit note); otherwise a plain <see cref="ReceiptInfo"/>.
        /// </summary>
        protected (ReceiptInfo, DeviceStatus) ExecutePrint(JObject prms, Receipt receipt, string? invoiceNumber)
        {
            var (json, status) = Request("printReceipt", prms);
            if (!status.Ok)
            {
                return (new ReceiptInfo(), status);
            }

            var (info, enrichedStatus) = EnrichReceiptInfo(json, status, receipt);

            if (invoiceNumber != null)
            {
                info = new InvoiceInfo
                {
                    ReceiptNumber = info.ReceiptNumber,
                    ReceiptDateTime = info.ReceiptDateTime,
                    ReceiptAmount = info.ReceiptAmount,
                    FiscalMemorySerialNumber = info.FiscalMemorySerialNumber,
                    InvoiceNumber = invoiceNumber
                };
            }

            return (info, enrichedStatus);
        }

        private static (ReceiptInfo, DeviceStatus) ErrorResult(StandardizedStatusMessageException e)
        {
            var status = new DeviceStatus();
            status.AddError(e.Code, e.Message);
            return (new ReceiptInfo(), status);
        }

        /// <summary>
        /// After a successful fiscal receipt, resolves the exact receipt number, amount, date and fiscal
        /// memory number. The printReceipt answer already carries this inline as a "qrcode" field, so it is
        /// used directly (no extra round-trip). If it is missing, the last QR code is read separately, and
        /// as a final fallback the plain printReceipt answer fields are used.
        /// </summary>
        protected (ReceiptInfo, DeviceStatus) EnrichReceiptInfo(JObject printResponse, DeviceStatus status, Receipt receipt)
        {
            // Preferred: the QR code returned inline with the printReceipt answer (no extra round-trip).
            if (ParseQrCode(printResponse.Value<string>("qrcode")) is ReceiptInfo inlineInfo)
            {
                return (inlineInfo, status);
            }

            // Fallback: read the last QR code explicitly.
            var (qrJson, qrStatus) = Request(
                "getData",
                null,
                new JObject { ["period"] = "day", ["type"] = "LastQRCode" });

            if (qrStatus.Ok && ParseQrCode(qrJson["response"]?.Value<string>("data")) is ReceiptInfo qrInfo)
            {
                return (qrInfo, status);
            }

            // Final fallback: the plain printReceipt answer fields.
            var fallback = new ReceiptInfo
            {
                FiscalMemorySerialNumber = Info.FiscalMemorySerialNumber,
                ReceiptNumber = printResponse.Value<string>("grandReceiptNum") ?? string.Empty,
                ReceiptAmount = ComputeTotal(receipt)
            };

            var ts = printResponse.Value<string>("receiptTimestamp");
            if (!string.IsNullOrEmpty(ts) && TryParseReceiptTimestamp(ts!, out var dt))
            {
                fallback.ReceiptDateTime = dt;
            }

            return (fallback, status);
        }

        /// <summary>
        /// Parses a QR code payload in the format FM*number*date*time*amount
        /// (e.g. 51019395*0000000201*2024-11-25*11:04:30*4.75) into a <see cref="ReceiptInfo"/>.
        /// Returns null when the payload is empty or does not have the expected structure.
        /// </summary>
        protected static ReceiptInfo? ParseQrCode(string? qrCode)
        {
            if (string.IsNullOrEmpty(qrCode))
            {
                return null;
            }

            var fields = qrCode.Split('*');
            if (fields.Length < 5)
            {
                return null;
            }

            var info = new ReceiptInfo
            {
                FiscalMemorySerialNumber = fields[0],
                ReceiptNumber = fields[1]
            };

            if (DateTime.TryParseExact($"{fields[2]} {fields[3]}", "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var receiptDateTime))
            {
                info.ReceiptDateTime = receiptDateTime;
            }

            if (decimal.TryParse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var receiptAmount))
            {
                info.ReceiptAmount = receiptAmount;
            }

            return info;
        }

        public override DeviceStatus PrintZReport(Credentials credentials)
        {
            var (_, status) = Request("printZReport");
            return status;
        }

        public override DeviceStatus PrintXReport(Credentials credentials)
        {
            var (_, status) = Request("printXReport");
            return status;
        }

        public override DeviceStatus PrintDuplicate(Credentials credentials)
        {
            var (_, status) = Request("printDuplicate");
            return status;
        }

        public override DeviceStatusWithRawResponse RawRequest(RequestFrame requestFrame)
        {
            var status = new DeviceStatus();
            var raw = string.Empty;
            JObject request;
            try
            {
                request = JObject.Parse(requestFrame.RawRequest);
            }
            catch
            {
                status.AddError("E401", "RawRequest must be a valid JSON-RPC object");
                return new DeviceStatusWithRawResponse(status) { RawResponse = raw };
            }

            try
            {
                JObject json;
                (json, raw) = RawJsonRequest(request);
                status = ParseResponseStatus(json);
            }
            catch (Exception ex)
            {
                status.AddError("E999", ex.Message);
            }

            return new DeviceStatusWithRawResponse(status) { RawResponse = raw };
        }

        public override DeviceStatusWithDateTime Reset(Credentials credentials)
        {
            // Per the SIS spec, getError cancels any pending/open receipt on the device.
            Request("getError");
            return CheckStatus();
        }
    }
}
