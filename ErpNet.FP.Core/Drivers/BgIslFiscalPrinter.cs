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

        public override DeviceStatusEx CheckStatus()
        {
            var (dateTime, status) = GetDateTime();
            var statusEx = new DeviceStatusEx(status);
            if (dateTime.HasValue)
            {
                statusEx.DateTime = dateTime.Value;
            }
            else
            {
                statusEx.Statuses.Add("Error occured while reading current status");
                statusEx.Errors.Add("Cannot read current date and time");
            }
            return statusEx;
        }

        public override DeviceStatus SetDateTime(DateTime dateTime)
        {
            var (_, status) = SetDeviceDateTime(dateTime);
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
            System.Diagnostics.Trace.WriteLine("PrintMoneyDeposit: {0}", response);
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(decimal amount)
        {
            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("withdraw amount must be positive number");
            }
            var (response, status) = MoneyTransfer(-amount);
            System.Diagnostics.Trace.WriteLine("PrintMoneyWithdraw: {0}", response);
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

            var (fiscalMemorySerialNumber, deviceStatus) = GetFiscalMemorySerialNumber();
            if (!deviceStatus.Ok)
            {
                return (receiptInfo, deviceStatus);
            }

            receiptInfo.FiscalMemorySerialNumber = fiscalMemorySerialNumber;

            // Opening receipt
            (_, deviceStatus) = OpenReceipt(receipt.UniqueSaleNumber);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while opening new fiscal receipt");
                return (receiptInfo, deviceStatus);
            }

            // Printing receipt's body
            deviceStatus = PrintReceiptBody(receipt);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.Statuses.Add($"Error occured while printing receipt items");
                return (receiptInfo, deviceStatus);
            }

            // Get the receipt date and time (current fiscal device date and time)
            DateTime? dateTime;
            (dateTime, deviceStatus) = GetDateTime();
            if (!dateTime.HasValue || !deviceStatus.Ok)
            {
                AbortReceipt();
                return (receiptInfo, deviceStatus);
            }
            receiptInfo.ReceiptDateTime = dateTime.Value;

            // Closing receipt
            string closeReceiptResponse;
            (closeReceiptResponse, deviceStatus) = CloseReceipt();
            if (!deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                deviceStatus.Statuses.Add($"Error occurred while closing the receipt");
                return (receiptInfo, deviceStatus);
            }

            // Get receipt number
            string lastDocumentNumberResponse;
            (lastDocumentNumberResponse, deviceStatus) = GetLastDocumentNumber(closeReceiptResponse);
            if (!deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                deviceStatus.Statuses.Add($"Error occurred while reading last document number");
                return (receiptInfo, deviceStatus);
            }
            receiptInfo.ReceiptNumber = lastDocumentNumberResponse;

            // Get receipt amount
            decimal? receiptAmount;
            (receiptAmount, deviceStatus) = GetReceiptAmount();
            if (!receiptAmount.HasValue || !deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                return (receiptInfo, deviceStatus);
            }
            receiptInfo.ReceiptAmount = receiptAmount.Value;

            return (receiptInfo, deviceStatus);
        }

        public override DeviceStatus PrintZeroingReport()
        {
            var (response, status) = PrintDailyReport();
            System.Diagnostics.Trace.WriteLine("PrintZeroingReport: {0}", response);
            // 0000,0.00,273.60,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00
            return status;
        }
    }
}
