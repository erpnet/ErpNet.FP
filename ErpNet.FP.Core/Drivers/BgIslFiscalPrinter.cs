using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer using the ISL implementation.
    /// </summary>
    /// <seealso cref="ErpNet.FP.BgFiscalPrinter" />
    public abstract partial class BgIslFiscalPrinter : BgFiscalPrinter
    {
        protected BgIslFiscalPrinter(IChannel channel, IDictionary<string, string>? options = null)
        : base(channel, options) { }

        public override DeviceStatus CheckStatus()
        {
            var (_, status) = GetStatus();
            return status;
        }

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
                    throw new ArgumentOutOfRangeException($"tax group {taxGroup} unsupported");
            }
        }

        public override DeviceStatus PrintMoneyDeposit(decimal amount)
        {
            var (response, status) = MoneyTransfer(amount);
            System.Diagnostics.Debug.WriteLine("PrintMoneyDeposit: {0}", response);
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(decimal amount)
        {
            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("withdraw amount must be positive number");
            }
            var (response, status) = MoneyTransfer(-amount);
            System.Diagnostics.Debug.WriteLine("PrintMoneyWithdraw: {0}", response);
            return status;
        }

        public virtual DeviceStatus PrintReceiptBody(Receipt receipt)
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
                        deviceStatus.Statuses.Add($"Error occurred in Item {itemNumber}");
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
                (_, deviceStatus) = FullPayment();
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
            }

            // Receipt finalization
            (_, deviceStatus) = CloseReceipt();
            if (!deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                deviceStatus.Statuses.Add($"Error occurred while closing the receipt");
                return deviceStatus;
            }

            return deviceStatus;
        }

        protected virtual (ReceiptInfo, DeviceStatus) GetLastReceiptInfo()
        {
            // QR Code Data Format: <FM Number>*<Receipt Number>*<Receipt Date>*<Receipt Hour>*<Receipt Amount>
            var (qrData, deviceStatus) = GetLastReceiptQRCodeData();
            if (!deviceStatus.Ok)
            {
                deviceStatus.Statuses.Add($"Error occurred while reading last receipt QR code data");
                return (new ReceiptInfo(), deviceStatus);
            }

            System.Diagnostics.Debug.WriteLine($"QRData: {qrData}");

            var qrDataFields = qrData.Split(',');
            if (qrDataFields.Length != 2)
            {
                deviceStatus.Errors.Add("Last receipt info should be splittable in two parts by comma.");
                deviceStatus.Statuses.Add($"Error occurred while parsing last receipt QR code data");
                return (new ReceiptInfo(), deviceStatus);
            }

            var qrCodeFields = qrDataFields[1].Split('*');
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
            catch (ArgumentNullException e)
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
            System.Diagnostics.Debug.WriteLine("PrintZeroingReport: {0}", response);
            // 0000,0.00,273.60,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00
            return status;
        }
    }
}
