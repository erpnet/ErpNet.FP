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
        protected BgZfpFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options) { }

        public override string GetPaymentTypeText(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Cash:
                    return "0";
                case PaymentType.BankTransfer:
                    return "1";
                case PaymentType.DebitCard:
                    return "2";
                case PaymentType.NationalHealthInsuranceFund:
                    return "3";
                case PaymentType.Voucher:
                    return "4";
                case PaymentType.Coupon:
                    return "5";
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
                        return (receiptInfo, deviceStatus);
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
                        item.PriceModifierType
                    );
                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.Statuses.Add($"Error occurred in Item {itemNumber}");
                        return (receiptInfo, deviceStatus);
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
                    return (receiptInfo, deviceStatus);
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
                        return (receiptInfo, deviceStatus);
                    }
                }
                (_, deviceStatus) = CloseReceipt();
                if (!deviceStatus.Ok)
                {
                    (_, deviceStatus) = AbortReceipt();
                    deviceStatus.Statuses.Add($"Error occurred while closing the receipt");
                    return (receiptInfo, deviceStatus);
                }
            }

            // QR Code Data Format: <FM Number>*<Receipt Number>*<Receipt Date>*<Receipt Hour>*<Receipt Amount>
            string qrCodeData;
            (qrCodeData, deviceStatus) = GetLastReceiptQRCodeData();
            if (!deviceStatus.Ok)
            {
                deviceStatus.Statuses.Add($"Error occurred while reading the receipt number.");
                return (receiptInfo, deviceStatus);
            }

            var qrCodeFields = qrCodeData.Split('*');
            receiptInfo.FiscalMemorySerialNumber = qrCodeFields[0];
            receiptInfo.ReceiptNumber = qrCodeFields[1];
            receiptInfo.ReceiptDate = qrCodeFields[2];
            receiptInfo.ReceiptTime = qrCodeFields[3];

            return (receiptInfo, deviceStatus);
        }

        public override DeviceStatus PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public override DeviceStatus PrintZeroingReport()
        {
            var (response, status) = PrintDailyReport();
            System.Diagnostics.Debug.WriteLine($"PrintDailyReport: {response}");
            return status;
        }
    }
}
