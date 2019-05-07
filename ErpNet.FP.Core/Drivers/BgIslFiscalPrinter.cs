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

            var deviceStatus = new DeviceStatus();

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
                    catch (Exception e)
                    {
                        deviceStatus = new DeviceStatus();
                        deviceStatus.Statuses.Add($"Error occured while in Item {itemNumber}");
                        deviceStatus.Errors.Add(e.Message);
                        return deviceStatus;
                    }
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

            return deviceStatus;
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

            deviceStatus = PrintReceiptBody(reversalReceipt);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while printing receipt items");
                return deviceStatus;
            }

            // Receipt finalization
            (_, deviceStatus) = CloseReceipt();
            return deviceStatus;
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

            string dateTimeResponse;
            (dateTimeResponse, deviceStatus) = GetDateTime();
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while reading current date and time");
                return (receiptInfo, deviceStatus);
            }


            try
            {
                receiptInfo.ReceiptDateTime = DateTime.ParseExact(dateTimeResponse,
                    "dd-MM-yy HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while parsing current date and time");
                return (receiptInfo, deviceStatus);
            }

            deviceStatus = PrintReceiptBody(receipt);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while printing receipt items");
                return (receiptInfo, deviceStatus);
            }

            // Receipt finalization
            (_, deviceStatus) = CloseReceipt();
            if (!deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                deviceStatus.Statuses.Add($"Error occurred while closing the receipt");
                return (receiptInfo, deviceStatus);
            }

            string lastDocumentNumberResponse;
            (lastDocumentNumberResponse, deviceStatus) = GetLastDocumentNumber();
            if (!deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                deviceStatus.Statuses.Add($"Error occurred while reading last document number");
                return (receiptInfo, deviceStatus);
            }

            receiptInfo.ReceiptNumber = lastDocumentNumberResponse;

            string receiptStatusResponse;
            (receiptStatusResponse, deviceStatus) = GetReceiptStatus();
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while reading last receipt status");
                return (receiptInfo, deviceStatus);
            }

            var fields = receiptStatusResponse.Split(',');
            if (fields.Length < 3)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while parsing last receipt status");
                deviceStatus.Errors.Add("Wrong format of receipt status");
                return (receiptInfo, deviceStatus);
            }

            try
            {
                var amountString = fields[2];
                if (amountString.Length > 0)
                {
                    switch (amountString[0])
                    {
                        case '+':
                            receiptInfo.ReceiptAmount = decimal.Parse(amountString.Substring(1), System.Globalization.CultureInfo.InvariantCulture) / 100m;
                            break;
                        case '-':
                            receiptInfo.ReceiptAmount = -decimal.Parse(amountString.Substring(1), System.Globalization.CultureInfo.InvariantCulture) / 100m;
                            break;
                        default:
                            receiptInfo.ReceiptAmount = decimal.Parse(amountString, System.Globalization.CultureInfo.InvariantCulture);
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                AbortReceipt();
                deviceStatus = new DeviceStatus();
                deviceStatus.Statuses.Add($"Error occured while parsing amount of last receipt status");
                deviceStatus.Errors.Add(e.Message);
                return (receiptInfo, deviceStatus);
            }

            return (receiptInfo, deviceStatus);
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
