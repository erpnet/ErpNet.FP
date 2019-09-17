using System;
using System.Collections.Generic;
using System.Globalization;

namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Drivers.BgFiscalPrinter" />
    public partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        public BgZfpFiscalPrinter(IChannel channel, IDictionary<string, string>? options = null)
        : base(channel, options) { }

        public override string GetTaxGroupText(TaxGroup taxGroup)
        {
            switch (taxGroup)
            {
                case TaxGroup.TaxGroup1:
                    return "А";
                case TaxGroup.TaxGroup2:
                    return "Б";
                case TaxGroup.TaxGroup3:
                    return "В";
                case TaxGroup.TaxGroup4:
                    return "Г";
                case TaxGroup.TaxGroup5:
                    return "Д";
                case TaxGroup.TaxGroup6:
                    return "Е";
                case TaxGroup.TaxGroup7:
                    return "Ж";
                case TaxGroup.TaxGroup8:
                    return "З";
                default:
                    throw new StandardizedStatusMessageException($"Tax group {taxGroup} unsupported", "E411");
            }
        }

        public override IDictionary<PaymentType, string> GetPaymentTypeMappings()
        {
            return new Dictionary<PaymentType, string> {
                { PaymentType.Cash,          "0" },
                { PaymentType.Check,         "1" },
                { PaymentType.Coupons,       "2" },
                { PaymentType.ExtCoupons,    "3" },
                { PaymentType.Packaging,     "4" },
                { PaymentType.InternalUsage, "5" },
                { PaymentType.Damage,        "6" },
                { PaymentType.Card,          "7" },
                { PaymentType.Bank,          "8" },
                { PaymentType.Reserved1,     "9" },
                { PaymentType.Reserved2,    "10" }
            };
        }

        public override DeviceStatusWithDateTime CheckStatus()
        {
            var (dateTime, status) = GetDateTime();
            var statusEx = new DeviceStatusWithDateTime(status);
            if (dateTime.HasValue)
            {
                statusEx.DeviceDateTime = dateTime.Value;
            }
            else
            {
                statusEx.AddInfo("Error occured while reading current status");
                statusEx.AddError("E409", "Cannot read current date and time");
            }
            return statusEx;
        }

        public override DeviceStatus SetDateTime(CurrentDateTime currentDateTime)
        {
            var (_, status) = SetDeviceDateTime(currentDateTime.DeviceDateTime);
            return status;
        }

        public override DeviceStatus PrintMoneyDeposit(TransferAmount transferAmount)
        {
            var (_, status) = MoneyTransfer(transferAmount);
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(TransferAmount transferAmount)
        {
            if (transferAmount.Amount < 0m)
            {
                throw new StandardizedStatusMessageException("Withdraw amount must be positive number", "E403");
            }
            transferAmount.Amount = -transferAmount.Amount;
            var (response, status) = MoneyTransfer(transferAmount);
            return status;
        }

        protected virtual (ReceiptInfo, DeviceStatus) PrintReceiptBody(Receipt receipt)
        {
            var receiptInfo = new ReceiptInfo();

            var (fiscalMemorySerialNumber, deviceStatus) = GetFiscalMemorySerialNumber();
            if (!deviceStatus.Ok)
            {
                return (receiptInfo, deviceStatus);
            }

            receiptInfo.FiscalMemorySerialNumber = fiscalMemorySerialNumber;

            uint itemNumber = 0;
            // Receipt items
            if (receipt.Items != null) foreach (var item in receipt.Items)
                {
                    itemNumber++;
                    if (item.Type == ItemType.Comment)
                    {
                        (_, deviceStatus) = AddComment(item.Text);
                    }
                    else
                    {
                        try
                        {
                            (_, deviceStatus) = AddItem(
                                item.Text,
                                item.UnitPrice,
                                item.TaxGroup,
                                item.Quantity,
                                item.PriceModifierValue,
                                item.PriceModifierType);
                        }
                        catch (StandardizedStatusMessageException e)
                        {
                            deviceStatus = new DeviceStatus();
                            deviceStatus.AddError(e.Code, e.Message);
                        }
                    }
                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.AddInfo($"Error occurred in Item {itemNumber}");
                        return (receiptInfo, deviceStatus);
                    }
                }

            // Receipt payments
            if (receipt.Payments == null || receipt.Payments.Count == 0)
            {
                (_, deviceStatus) = FullPaymentAndCloseReceipt();
                if (!deviceStatus.Ok)
                {
                    AbortReceipt();
                    deviceStatus.AddInfo($"Error occurred while making full payment in cash and closing the receipt");
                    return (receiptInfo, deviceStatus);
                }
            }
            else
            {
                uint paymentNumber = 0;
                foreach (var payment in receipt.Payments)
                {
                    paymentNumber++;

                    if (payment.PaymentType == PaymentType.Change)
                    {
                        continue;
                    }

                    try
                    {
                        (_, deviceStatus) = AddPayment(payment.Amount, payment.PaymentType);
                    }
                    catch (StandardizedStatusMessageException e)
                    {
                        deviceStatus = new DeviceStatus();
                        deviceStatus.AddError(e.Code, e.Message);
                    }

                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.AddInfo($"Error occurred in Payment {paymentNumber}");
                        return (receiptInfo, deviceStatus);
                    }
                }
                (_, deviceStatus) = CloseReceipt();
                if (!deviceStatus.Ok)
                {
                    AbortReceipt();
                    deviceStatus.AddInfo($"Error occurred while closing the receipt");
                    return (receiptInfo, deviceStatus);
                }
            }

            return GetLastReceiptInfo();
        }

        protected virtual (ReceiptInfo, DeviceStatus) GetLastReceiptInfo()
        {
            // QR Code Data Format: <FM Number>*<Receipt Number>*<Receipt Date>*<Receipt Hour>*<Receipt Amount>
            var (qrCodeData, deviceStatus) = GetLastReceiptQRCodeData();
            if (!deviceStatus.Ok)
            {
                deviceStatus.AddInfo($"Error occurred while reading last receipt QR code data");
                return (new ReceiptInfo(), deviceStatus);
            }

            var qrCodeFields = qrCodeData.Split('*');
            return (new ReceiptInfo
            {
                ReceiptNumber = qrCodeFields[1],
                ReceiptDateTime = DateTime.ParseExact(string.Format(
                    $"{qrCodeFields[2]} {qrCodeFields[3]}"),
                    "yyyy-MM-dd HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture)
            }, deviceStatus);
        }

        public override (ReceiptInfo, DeviceStatus) PrintReversalReceipt(ReversalReceipt reversalReceipt)
        {
            // Abort all unfinished or erroneus receipts
            AbortReceipt();

            // Receipt header
            var (_, deviceStatus) = OpenReversalReceipt(
                reversalReceipt.Reason,
                reversalReceipt.ReceiptNumber,
                reversalReceipt.ReceiptDateTime,
                reversalReceipt.FiscalMemorySerialNumber,
                reversalReceipt.UniqueSaleNumber,
                reversalReceipt.Operator,
                reversalReceipt.OperatorPassword);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.AddInfo($"Error occured while opening new fiscal reversal receipt");
                return (new ReceiptInfo(), deviceStatus);
            }

            return PrintReceiptBody(reversalReceipt);
        }

        public override DeviceStatusWithCashAmount Cash(Credentials credentials)
        {
            var (response, status) = Request(CommandReadDailyAvailableAmounts, "0");
            var statusEx = new DeviceStatusWithCashAmount(status);
            var commaFields = response.Split(';');
            if (commaFields.Length < 3)
            {
                statusEx.AddInfo("Error occured while reading cash amount");
                statusEx.AddError("E409", "Invalid format");
            }
            else
            {
                var amountString = commaFields[1].Trim();
                if (amountString.Contains("."))
                {
                    statusEx.Amount = decimal.Parse(amountString, CultureInfo.InvariantCulture);
                }
                else
                {
                    statusEx.Amount = decimal.Parse(amountString, CultureInfo.InvariantCulture) / 100m;
                }
            }
            return statusEx;
        }

        public override (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
            // Abort all unfinished or erroneus receipts
            AbortReceipt();

            // Receipt header
            var (_, deviceStatus) = OpenReceipt(
                receipt.UniqueSaleNumber,
                receipt.Operator,
                receipt.OperatorPassword
            );
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.AddInfo($"Error occured while opening new fiscal receipt");
                return (new ReceiptInfo(), deviceStatus);
            }

            return PrintReceiptBody(receipt);
        }

        public override DeviceStatus PrintZReport(Credentials credentials)
        {
            var (response, status) = PrintDailyReport(true);
            return status;
        }

        public override DeviceStatus PrintXReport(Credentials credentials)
        {
            var (response, status) = PrintDailyReport(false);
            return status;
        }

        public override DeviceStatusWithDateTime Reset(Credentials credentials)
        {
            AbortReceipt();
            FullPaymentAndCloseReceipt();
            return CheckStatus();
        }
    }
}
