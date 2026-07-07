#nullable enable
namespace ErpNet.FP.Core.Drivers.BgSis
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Serilog;

    /// <summary>
    /// JSON-RPC plumbing and request building for the SIS Fiscal Module.
    /// The module speaks JSON-RPC 2.0 over plain HTTP POST; framing is handled by HTTP.
    /// </summary>
    public partial class BgSisJsonFiscalPrinter : BgFiscalPrinter
    {
        protected int idCounter = 0;
        protected const int MaxBusyRetries = 5;
        protected const int BusyRetryDelayMs = 500;

        protected static readonly Encoding JsonEncoding = new UTF8Encoding(false);

        /// <summary>
        /// Sends a JSON-RPC request object and returns the parsed and raw responses.
        /// Retries while the device replies "BUSY". Throws on transport/parse errors so that
        /// detection can skip incompatible channels.
        /// </summary>
        protected (JObject json, string raw) RawJsonRequest(JObject request)
        {
            var requestText = request.ToString(Formatting.None);
            var requestBytes = JsonEncoding.GetBytes(requestText);

            for (var attempt = 0; ; attempt++)
            {
                if (DeadLine < DateTime.Now)
                {
                    throw new TimeoutException("User timeout occured while sending the request");
                }

                // Only the write/read frame exchange is serialized. The BUSY back-off below sleeps
                // outside the lock so it does not block other requests to the same printer.
                string responseText;
                lock (frameSyncLock)
                {
                    Log.Information($"{Channel.Descriptor} <<< {requestText}");
                    Channel.Write(requestBytes);

                    responseText = JsonEncoding.GetString(Channel.Read()).Trim();
                    Log.Information($"{Channel.Descriptor} >>> {responseText}");
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    throw new InvalidResponseException(
                        $"Empty response from device for method '{request.Value<string>("method")}'");
                }

                JObject json;
                try
                {
                    json = JObject.Parse(responseText);
                }
                catch (JsonException ex)
                {
                    throw new InvalidResponseException($"Invalid JSON response: {responseText}", ex);
                }

                if (string.Equals(json.Value<string>("result"), "BUSY", StringComparison.OrdinalIgnoreCase))
                {
                    if (attempt >= MaxBusyRetries)
                    {
                        throw new TimeoutException("Device is busy (BUSY) after maximum retries");
                    }

                    Thread.Sleep(BusyRetryDelayMs);

                    continue;
                }

                return (json, responseText);
            }
        }

        /// <summary>
        /// Builds and sends a JSON-RPC request. <paramref name="topLevel"/> members (e.g. period/type
        /// for getData) are merged at the root, next to method/id, as the SIS spec expects.
        /// </summary>
        protected (JObject json, DeviceStatus status) Request(
            string method,
            JObject? @params = null,
            JObject? topLevel = null)
        {
            var request = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = Interlocked.Increment(ref idCounter),
                ["method"] = method
            };

            if (@params != null)
            {
                request["params"] = @params;
            }

            if (topLevel != null)
            {
                foreach (var prop in topLevel.Properties())
                    request[prop.Name] = prop.Value;
            }

            try
            {
                var (json, _) = RawJsonRequest(request);

                return (json, ParseResponseStatus(json));
            }
            catch (TimeoutException ex)
            {
                var status = new DeviceStatus();
                status.AddError("E999", ex.Message);

                return (new JObject(), status);
            }
            catch (InvalidResponseException ex)
            {
                var status = new DeviceStatus();
                status.AddError("E999", ex.Message);

                return (new JObject(), status);
            }
        }

        /// <summary>
        /// Maps a SIS response object to a <see cref="DeviceStatus"/>, inspecting both the MFC error
        /// (mfc_error/mfc_error_message) and the printer status array (prn_status). Note: a "result":"OK"
        /// for getStatus does NOT imply a healthy printer - paper/cover conditions live in prn_status.
        /// </summary>
        protected DeviceStatus ParseResponseStatus(JObject json)
        {
            var status = new DeviceStatus();

            // JSON-RPC level error object
            if (json["error"] is JObject errorObj)
            {
                status.AddError(
                    errorObj.Value<string>("code") ?? "E999",
                    errorObj.Value<string>("message") ?? "Unknown error");
            }

            // MFC (fiscal controller) error. The device reports a hardware-specific hex code and an
            // EM_* message. Surface them under the standardized "see device manual" code E999, so callers
            // still get a standard error code while the original code and message are kept in the text.
            var mfcError = json.Value<string>("mfc_error");
            var mfcErrorMessage = json.Value<string>("mfc_error_message") ?? string.Empty;
            if (!string.IsNullOrEmpty(mfcError)
                && mfcError != "0"
                && !mfcErrorMessage.Equals("EM_NO_ERROR", StringComparison.OrdinalIgnoreCase))
            {
                var detail = string.IsNullOrEmpty(mfcErrorMessage) ? "Device error" : mfcErrorMessage;
                status.AddError("E999", $"MFC error {mfcError}: {detail}");
            }

            // Printer hardware status (paper, cover, ...). The device may repeat the same status
            // (e.g. two "PaperEnd" entries), so de-duplicate before mapping.
            if (json["prn_status"] is JArray prnStatus)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in prnStatus)
                {
                    var s = entry.Value<string>("status");
                    if (!string.IsNullOrEmpty(s) && seen.Add(s))
                        MapPrinterStatus(status, s);
                }
            }

            // Fiscal controller status (informational)
            if (json["mfc_status"] is JArray mfcStatus)
            {
                foreach (var entry in mfcStatus)
                {
                    var s = entry.Value<string>("status");
                    if (!string.IsNullOrEmpty(s))
                        status.AddInfo($"MFC: {s}");
                }
            }

            // Pending NRA blocking
            var reason2block = json.Value<string>("reason2block");
            if (!string.IsNullOrEmpty(reason2block) && reason2block != "0")
            {
                var min2block = json.Value<string>("min2block") ?? string.Empty;
                status.AddWarning(
                    "W599",
                    $"Device pending NRA blocking (reason {reason2block}, {min2block} minutes left)");
            }

            var lastNraErr = json.Value<int?>("lastNRAErrNum");
            if (lastNraErr.HasValue && lastNraErr.Value != 0)
            {
                status.AddWarning(
                    "W599",
                    $"NRA error {lastNraErr.Value}: {json.Value<string>("lastNRAErrText") ?? string.Empty}");
            }

            return status;
        }

        protected void MapPrinterStatus(DeviceStatus status, string prnStatus)
        {
            switch (prnStatus.Replace(" ", string.Empty).ToUpperInvariant())
            {
                case "PAPEREND":
                    status.AddError("E301", "Paper end");
                    break;

                case "PAPERNEAREND":
                    status.AddWarning("W301", "Paper near end");
                    break;

                case "COVEROPEN":
                    status.AddError("E302", "Cover is open");
                    break;

                case "CUTTERERROR":
                    status.AddError("E306", "Error in paper cutter");
                    break;

                case "AUTORECOVERABLEERROR":
                    status.AddWarning("W399", "Printer reported an auto-recoverable error");
                    break;

                case "CASHDRAWEROPEN":
                case "CASHDRAWERCLOSED":
                    // Not a fault.
                    break;

                default:
                    status.AddInfo($"PRN: {prnStatus}");
                    break;
            }
        }

        protected override DeviceStatus ParseStatus(byte[]? status)
        {
            if (status == null || status.Length == 0)
            {
                return new DeviceStatus();
            }

            try
            {
                return ParseResponseStatus(JObject.Parse(JsonEncoding.GetString(status)));
            }
            catch
            {
                var s = new DeviceStatus();
                s.AddError("E999", "Cannot parse device status");

                return s;
            }
        }

        protected JObject BuildReceiptParams(Receipt receipt, ReversalReceipt? reversal)
        {
            var begin = new JObject();
            if (int.TryParse(receipt.Operator, out var operatorNumber))
            {
                begin["operatorNumber"] = operatorNumber;
            }
            else
            {
                begin["operatorNumber"] = 1;
                if (!string.IsNullOrEmpty(receipt.Operator))
                    begin["operatorName"] = receipt.Operator;
            }

            var posId = GetPosId();
            if (!string.IsNullOrEmpty(posId))
                begin["posId"] = posId;

            if (!string.IsNullOrEmpty(receipt.UniqueSaleNumber))
                begin["usn"] = receipt.UniqueSaleNumber;

            var prms = new JObject { ["beginFiscalReceiptInput"] = begin };

            var freeprint = new JArray();
            var footer = new JArray();
            var receiptItems = new JArray();
            var subtotal = new JArray();
            JObject? lastSaleItem = null;
            var anySale = false;

            if (receipt.Items != null)
            {
                foreach (var item in receipt.Items)
                {
                    switch (item.Type)
                    {
                        case ItemType.Sale:
                            var saleItem = BuildSaleItem(item);
                            receiptItems.Add(saleItem);
                            lastSaleItem = saleItem;
                            anySale = true;
                            break;

                        case ItemType.Comment:
                            if (!anySale || lastSaleItem == null)
                            {
                                freeprint.Add(new JObject { ["text"] = item.Text });
                            }
                            else
                            {
                                if (lastSaleItem["textlines"] is not JArray textlines)
                                {
                                    textlines = new JArray();
                                    lastSaleItem["textlines"] = textlines;
                                }
                                textlines.Add(new JObject { ["text"] = item.Text });
                            }
                            break;

                        case ItemType.FooterComment:
                            footer.Add(new JObject { ["text"] = item.Text, ["type"] = "text" });
                            break;

                        case ItemType.SurchargeAmount:
                        case ItemType.DiscountAmount:
                            // Subtotal (receipt-level) modifier. taxGroup is optional in the FP contract,
                            // but the SIS device requires enumVatCategory (SubTotalAmountModifiersRequireTaxGroup).
                            // Map the provided taxGroup 1:1; error if the device requires it and it is missing.
                            var subtotalEntry = new JObject
                            {
                                ["subtotalText"] = item.Text ?? string.Empty,
                                ["subtotalSurchargeAmount"] =
                                    (item.Type == ItemType.DiscountAmount ? -item.Amount : item.Amount)
                                        .ToString(CultureInfo.InvariantCulture)
                            };

                            if (item.TaxGroup != TaxGroup.Unspecified)
                            {
                                subtotalEntry["enumVatCategory"] =
                                    int.Parse(GetTaxGroupText(item.TaxGroup), CultureInfo.InvariantCulture);
                            }
                            else if (Info.SubTotalAmountModifiersRequireTaxGroup)
                            {
                                throw new StandardizedStatusMessageException(
                                    "Subtotal amount modifier requires a taxGroup for this device", "E411");
                            }

                            subtotal.Add(subtotalEntry);
                            break;
                    }
                }
            }

            if (freeprint.Count > 0)
                prms["freeprint"] = freeprint;

            prms["receiptItems"] = receiptItems;

            if (subtotal.Count > 0)
                prms["subtotal"] = subtotal;

            prms["receiptPayments"] = BuildPayments(receipt, reversal != null);

            if (reversal != null)
            {
                prms["stornoInput"] = new JObject
                {
                    ["documentDate"] = FormatStornoDate(reversal.ReceiptDateTime),
                    ["enumStornoType"] = int.Parse(GetReversalReasonText(reversal.Reason), CultureInfo.InvariantCulture),
                    ["fiscMemNumber"] = reversal.FiscalMemorySerialNumber,
                    ["receiptNumber"] = reversal.ReceiptNumber
                };
            }

            if (footer.Count > 0)
                prms["textAfterPayment"] = footer;

            return prms;
        }

        protected JObject BuildSaleItem(Item item)
        {
            var quantity = item.Quantity == 0m ? 1m : item.Quantity;
            var jItem = new JObject
            {
                ["description"] = item.Text,
                ["enumVatCategory"] = int.Parse(GetTaxGroupText(item.TaxGroup), CultureInfo.InvariantCulture),
                ["price"] = item.UnitPrice.ToString(CultureInfo.InvariantCulture),
                ["quantity"] = quantity.ToString(CultureInfo.InvariantCulture)
            };

            if (item.Department > 0)
                jItem["department"] = item.Department;

            if (item.PriceModifierType != PriceModifierType.None && item.PriceModifierValue != 0m)
            {
                var itemSum = Math.Round(quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);
                var amount = item.PriceModifierType switch
                {
                    PriceModifierType.DiscountAmount => -item.PriceModifierValue,
                    PriceModifierType.SurchargeAmount => item.PriceModifierValue,
                    PriceModifierType.DiscountPercent =>
                        -Math.Round(itemSum * item.PriceModifierValue / 100m, 2, MidpointRounding.AwayFromZero),
                    PriceModifierType.SurchargePercent =>
                        Math.Round(itemSum * item.PriceModifierValue / 100m, 2, MidpointRounding.AwayFromZero),
                    _ => 0m
                };

                jItem["surchargeAmount"] = amount.ToString(CultureInfo.InvariantCulture);
            }

            return jItem;
        }

        protected JArray BuildPayments(Receipt receipt, bool isReversal)
        {
            var payments = new JArray();

            // Reversal receipts only accept cash; when no payments are given, pay the full total in cash.
            if (isReversal || receipt.Payments == null || receipt.Payments.Count == 0)
            {
                payments.Add(new JObject
                {
                    ["amount"] = ComputeTotal(receipt).ToString(CultureInfo.InvariantCulture),
                    ["medium"] = 0
                });

                return payments;
            }

            var change = 0m;
            foreach (var payment in receipt.Payments)
            {
                if (payment.PaymentType == PaymentType.Change)
                {
                    change += -payment.Amount;
                    continue;
                }

                payments.Add(new JObject
                {
                    ["amount"] = payment.Amount.ToString(CultureInfo.InvariantCulture),
                    ["medium"] = int.Parse(GetPaymentTypeText(payment.PaymentType), CultureInfo.InvariantCulture)
                });
            }

            if (change > 0m)
            {
                foreach (var payment in payments)
                {
                    if (payment.Value<int>("medium") == 0)
                    {
                        payment["change"] = change.ToString(CultureInfo.InvariantCulture);
                        break;
                    }
                }
            }

            return payments;
        }

        protected static decimal ComputeTotal(Receipt receipt)
        {
            var total = 0m;
            if (receipt.Items != null)
            {
                foreach (var item in receipt.Items)
                {
                    switch (item.Type)
                    {
                        case ItemType.Sale:
                            var quantity = item.Quantity == 0m ? 1m : item.Quantity;
                            var sum = Math.Round(quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);
                            switch (item.PriceModifierType)
                            {
                                case PriceModifierType.DiscountAmount:
                                    sum -= item.PriceModifierValue;
                                    break;

                                case PriceModifierType.SurchargeAmount:
                                    sum += item.PriceModifierValue;
                                    break;

                                case PriceModifierType.DiscountPercent:
                                    sum -= Math.Round(sum * item.PriceModifierValue / 100m, 2, MidpointRounding.AwayFromZero);
                                    break;

                                case PriceModifierType.SurchargePercent:
                                    sum += Math.Round(sum * item.PriceModifierValue / 100m, 2, MidpointRounding.AwayFromZero);
                                    break;
                            }
                            total += sum;
                            break;

                        case ItemType.SurchargeAmount:
                            total += item.Amount;
                            break;

                        case ItemType.DiscountAmount:
                            total -= item.Amount;
                            break;
                    }
                }
            }

            return Math.Round(total, 2, MidpointRounding.AwayFromZero);
        }

        // SIS storno timestamp format: "ss,mm,hh;DD,MM,YY"
        protected static string FormatStornoDate(DateTime dt)
        {
            return dt.ToString("ss,mm,HH;dd,MM,yy", CultureInfo.InvariantCulture);
        }

        // SIS receipt timestamp format (printReceipt answer): "ss,mm,hh;DD,MM,YY"
        protected static bool TryParseReceiptTimestamp(string ts, out DateTime dt)
        {
            dt = default;
            var parts = ts.Split(';');
            if (parts.Length != 2)
            {
                return false;
            }

            var time = parts[0].Split(',');
            var date = parts[1].Split(',');
            if (time.Length != 3 || date.Length != 3)
            {
                return false;
            }

            try
            {
                dt = new DateTime(
                    2000 + int.Parse(date[2], CultureInfo.InvariantCulture),
                    int.Parse(date[1], CultureInfo.InvariantCulture),
                    int.Parse(date[0], CultureInfo.InvariantCulture),
                    int.Parse(time[2], CultureInfo.InvariantCulture),
                    int.Parse(time[1], CultureInfo.InvariantCulture),
                    int.Parse(time[0], CultureInfo.InvariantCulture));

                return true;
            }
            catch
            {
                return false;
            }
        }

        public (string raw, DeviceStatus status) GetRawDeviceInfo()
        {
            var (_, raw) = RawJsonRequest(new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = Interlocked.Increment(ref idCounter),
                ["method"] = "getMfcInfo"
            });

            return (raw, new DeviceStatus());
        }

        public (string model, string fwChecksum) GetModelAndChecksum()
        {
            var (json, _) = Request("getStatus");
            return (json.Value<string>("printerModel") ?? string.Empty,
                    json.Value<string>("fwChecksum") ?? string.Empty);
        }
    }
}
