using System;
using System.Collections.Generic;

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
                    return "À";
                case TaxGroup.TaxGroup2:
                    return "Á";
                case TaxGroup.TaxGroup3:
                    return "Â";
                case TaxGroup.TaxGroup4:
                    return "Ã";
                case TaxGroup.TaxGroup5:
                    return "Ä";
                case TaxGroup.TaxGroup6:
                    return "Å";
                case TaxGroup.TaxGroup7:
                    return "Æ";
                case TaxGroup.TaxGroup8:
                    return "Ç";
                default:
                    throw new StandardizedResponseException($"Tax group {taxGroup} unsupported", "E411");
            }
        }

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "0";
                case PaymentType.Card:
                    return "7";
                case PaymentType.Check:
                    return "1";
                case PaymentType.Packaging:
                    return "4";
                case PaymentType.Reserved1:
                    return "9";
                case PaymentType.Reserved2:
                    return "10";
                default:
                    throw new StandardizedResponseException($"Payment type {paymentType} unsupported", "E406");
            }
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
            System.Diagnostics.Trace.WriteLine($"PrintMoneyDeposit: {transferAmount}");
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(TransferAmount transferAmount)
        {
            if (transferAmount.Amount < 0m)
            {
                throw new StandardizedResponseException("Withdraw amount must be positive number", "E403");
            }
            transferAmount.Amount = -transferAmount.Amount;
            var (response, status) = MoneyTransfer(transferAmount);
            System.Diagnostics.Trace.WriteLine($"PrintMoneyWithdraw: {response}");
            return status;
        }

        protected virtual DeviceStatus PrintReceiptBody(Receipt receipt)
        {
            if (receipt.Items == null || receipt.Items.Count == 0)
            {
                throw new StandardizedResponseException("Receipt.Items must be not null or empty", "E410");
            }

            DeviceStatus deviceStatus;

            uint itemNumber = 0;
            // Receipt items
            foreach (var item in receipt.Items)
            {
                itemNumber++;
                if (item.IsComment)
                {
                    (_, deviceStatus) = AddComment(item.Text);
                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.AddInfo($"Error occurred in the comment of Item {itemNumber}");
                        return deviceStatus;
                    }
                }
                else
                {
                    if (item.PriceModifierValue < 0m)
                    {
                        throw new StandardizedResponseException("PriceModifierValue amount must be positive number", "E403");
                    }
                    if (item.PriceModifierValue != 0m && item.PriceModifierType == PriceModifierType.None)
                    {
                        throw new StandardizedResponseException("PriceModifierValue must be 0 if priceModifierType is None", "E403");
                    }
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
                    catch (StandardizedResponseException e)
                    {
                        deviceStatus = new DeviceStatus();
                        deviceStatus.AddError(e.Code, e.Message);
                        return deviceStatus;
                    }
                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.AddInfo($"Error occurred in Item {itemNumber}");
                        return deviceStatus;
                    }
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
                    return deviceStatus;
                }
            }
            else
            {
                uint paymentNumber = 0;
                foreach (var payment in receipt.Payments)
                {
                    paymentNumber++;
                    try
                    {
                        (_, deviceStatus) = AddPayment(payment.Amount, payment.PaymentType);
                    }
                    catch (StandardizedResponseException e)
                    {
                        deviceStatus = new DeviceStatus();
                        deviceStatus.AddError(e.Code, e.Message);
                        return deviceStatus;
                    }
                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.AddInfo($"Error occurred in Payment {paymentNumber}");
                        return deviceStatus;
                    }
                }
                (_, deviceStatus) = CloseReceipt();
                if (!deviceStatus.Ok)
                {
                    AbortReceipt();
                    deviceStatus.AddInfo($"Error occurred while closing the receipt");
                    return deviceStatus;
                }
            }

            return deviceStatus;
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

        public override DeviceStatus PrintReversalReceipt(ReversalReceipt reversalReceipt)
        {
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
                return deviceStatus;
            }

            try
            {
                return PrintReceiptBody(reversalReceipt);
            }
            catch (StandardizedResponseException e)
            {
                deviceStatus = new DeviceStatus();
                deviceStatus.AddError(e.Code, e.Message);
                return deviceStatus;
            }
        }

        public override (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
            var receiptInfo = new ReceiptInfo();

            var (fiscalMemorySerialNumber, deviceStatus) = GetFiscalMemorySerialNumber();
            if (!deviceStatus.Ok)
            {
                return (receiptInfo, deviceStatus);
            }

            receiptInfo.FiscalMemorySerialNumber = fiscalMemorySerialNumber;

            // Receipt header
            (_, deviceStatus) = OpenReceipt(
                receipt.UniqueSaleNumber,
                receipt.Operator,
                receipt.OperatorPassword
            );
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.AddInfo($"Error occured while opening new fiscal receipt");
                return (receiptInfo, deviceStatus);
            }

            try
            {
                deviceStatus = PrintReceiptBody(receipt);
                if (!deviceStatus.Ok)
                {
                    return (receiptInfo, deviceStatus);
                }
            }
            catch (StandardizedResponseException e)
            {
                AbortReceipt();
                deviceStatus = new DeviceStatus();
                deviceStatus.AddError(e.Code, e.Message);
                return (receiptInfo, deviceStatus);
            }

            return GetLastReceiptInfo();
        }

        public override DeviceStatus PrintZReport(Credentials credentials)
        {
            var (response, status) = PrintDailyReport(true);
            System.Diagnostics.Trace.WriteLine($"PrintDailyReport: {response}");
            return status;
        }

        public override DeviceStatus PrintXReport(Credentials credentials)
        {
            var (response, status) = PrintDailyReport(false);
            System.Diagnostics.Trace.WriteLine($"PrintDailyReport: {response}");
            return status;
        }
    }
}
