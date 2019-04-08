using ErpNet.FP.Core;
using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer using the Zfp implementation.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Drivers.BgFiscalPrinter" />
    public abstract partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        protected BgZfpFiscalPrinter(IChannel channel, IDictionary<string, string>? options = null)
        : base(channel, options) { }

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "0";
                case PaymentType.Check:
                    return "1";
                case PaymentType.Coupon:
                    return "2";
                case PaymentType.Voucher:
                    return "3";
                case PaymentType.Card:
                    return "7";
                case PaymentType.Bank:
                    return "8";
                case PaymentType.Reserved1:
                    return "9";
                case PaymentType.Reserved2:
                    return "10";
                default:
                    return "0";
            }
        }

        public override DeviceStatus CheckStatus()
        {
            var (_, status) = GetStatus();
            return status;
        }

        public override DeviceStatus PrintMoneyDeposit(decimal amount)
        {
            var (response, status) = MoneyTransfer(amount);
            System.Diagnostics.Debug.WriteLine($"PrintMoneyDeposit: {response}");
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(decimal amount)
        {
            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("withdraw amount must be positive number");
            }
            var (response, status) = MoneyTransfer(-amount);
            System.Diagnostics.Debug.WriteLine($"PrintMoneyWithdraw: {response}");
            return status;
        }

        protected virtual DeviceStatus PrintReceiptBody(Receipt receipt)
        {
            if (receipt.Items == null || receipt.Items.Count == 0)
            {
                throw new ArgumentNullException("receipt.Items must be not null or empty");
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
                        deviceStatus.Statuses.Add($"Error occurred in the comment of Item {itemNumber}");
                        return deviceStatus;
                    }
                }
                else
                {
                    if (item.PriceModifierValue < 0m)
                    {
                        throw new ArgumentOutOfRangeException("priceModifierValue amount must be positive number");
                    }
                    if (item.PriceModifierValue != 0m && item.PriceModifierType == PriceModifierType.None)
                    {
                        throw new ArgumentOutOfRangeException("priceModifierValue must be 0 if priceModifierType is None");
                    }
                    (_, deviceStatus) = AddItem(
                        item.Text,
                        item.UnitPrice,
                        item.TaxGroup,
                        item.Quantity,
                        item.PriceModifierValue,
                        item.PriceModifierType);
                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.Statuses.Add($"Error occurred in Item {itemNumber}");
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
                    deviceStatus.Statuses.Add($"Error occurred while making full payment in cash and closing the receipt");
                    return deviceStatus;
                }
            }
            else
            {
                uint paymentNumber = 0;
                foreach (var payment in receipt.Payments)
                {
                    paymentNumber++;
                    (_, deviceStatus) = AddPayment(payment.Amount, payment.PaymentType);
                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.Statuses.Add($"Error occurred in Payment {paymentNumber}");
                        return deviceStatus;
                    }
                }
                (_, deviceStatus) = CloseReceipt();
                if (!deviceStatus.Ok)
                {
                    (_, deviceStatus) = AbortReceipt();
                    deviceStatus.Statuses.Add($"Error occurred while closing the receipt");
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
                deviceStatus.Statuses.Add($"Error occurred while reading last receipt QR code data");
                return (new ReceiptInfo(), deviceStatus);
            }

            var qrCodeFields = qrCodeData.Split('*');
            return (new ReceiptInfo
            {
                FiscalMemorySerialNumber = qrCodeFields[0],
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
                reversalReceipt.UniqueSaleNumber);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while opening new fiscal reversal receipt");
                return deviceStatus;
            }

            try
            {
                return PrintReceiptBody(reversalReceipt);
            }
            catch(ArgumentNullException e)
            {
                AbortReceipt();
                deviceStatus = new DeviceStatus();
                deviceStatus.Statuses.Add($"Error occured while printing receipt items");
                deviceStatus.Errors.Add(e.Message);
                return deviceStatus;
            }
        }

        public override (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
            var receiptInfo = new ReceiptInfo();
            // Receipt header
            var (_, deviceStatus) = OpenReceipt(receipt.UniqueSaleNumber);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while opening new fiscal receipt");
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
            catch (ArgumentNullException e)
            {
                AbortReceipt();
                deviceStatus = new DeviceStatus();
                deviceStatus.Statuses.Add($"Error occured while printing receipt items");
                deviceStatus.Errors.Add(e.Message);
                return (receiptInfo, deviceStatus);
            }

            return GetLastReceiptInfo();
        }

        public override DeviceStatus PrintZeroingReport()
        {
            var (response, status) = PrintDailyReport();
            System.Diagnostics.Debug.WriteLine($"PrintDailyReport: {response}");
            return status;
        }
    }
}
