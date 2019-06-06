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
                statusEx.AddError("Å409", "Cannot read current date and time");
            }
            return statusEx;
        }

        public override DeviceStatus SetDateTime(CurrentDateTime currentDateTime)
        {
            var (_, status) = SetDeviceDateTime(currentDateTime.DeviceDateTime);
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
                    throw new StandardizedStatusMessageException($"Tax group {taxGroup} unsupported", "E411");
            }
        }

        public override DeviceStatus PrintMoneyDeposit(TransferAmount transferAmount)
        {
            var (response, status) = MoneyTransfer(transferAmount.Amount);
            System.Diagnostics.Trace.WriteLine("PrintMoneyDeposit: {0}", response);
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(TransferAmount transferAmount)
        {
            if (transferAmount.Amount < 0m)
            {
                throw new StandardizedStatusMessageException("Withdraw amount must be positive number", "E403");
            }
            var (response, status) = MoneyTransfer(-transferAmount.Amount);
            System.Diagnostics.Trace.WriteLine("PrintMoneyWithdraw: {0}", response);
            return status;
        }

        public virtual DeviceStatus PrintReceiptBody(Receipt receipt)
        {
            if (receipt.Items == null || receipt.Items.Count == 0)
            {
                throw new StandardizedStatusMessageException("Receipt.Items must be not null or empty", "E410");
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
                        deviceStatus.AddInfo($"Error occurred in Item {itemNumber}");
                        return deviceStatus;
                    }
                }
                else
                {
                    if (item.PriceModifierValue < 0m)
                    {
                        throw new StandardizedStatusMessageException("PriceModifierValue amount must be positive number", "E403");
                    }
                    if (item.PriceModifierValue != 0m && item.PriceModifierType == PriceModifierType.None)
                    {
                        throw new StandardizedStatusMessageException("PriceModifierValue must be 0 if priceModifierType is None", "E403");
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
                    catch (StandardizedStatusMessageException e)
                    {
                        deviceStatus = new DeviceStatus();
                        deviceStatus.AddError(e.Code, e.Message);
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
                (_, deviceStatus) = FullPayment();
                if (!deviceStatus.Ok)
                {
                    AbortReceipt();
                    deviceStatus.AddInfo($"Error occurred while making full payment in cash");
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
                    catch (StandardizedStatusMessageException e)
                    {
                        deviceStatus = new DeviceStatus();
                        deviceStatus.AddError(e.Code, e.Message);
                    }
                    if (!deviceStatus.Ok)
                    {
                        AbortReceipt();
                        deviceStatus.AddInfo($"Error occurred in Payment {paymentNumber}");
                        return deviceStatus;
                    }
                }
            }

            return deviceStatus;
        }


        public override DeviceStatus PrintReversalReceipt(ReversalReceipt reversalReceipt)
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
                return deviceStatus;
            }

            deviceStatus = PrintReceiptBody(reversalReceipt);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.AddInfo($"Error occured while printing receipt items");
                return deviceStatus;
            }

            // Receipt finalization
            (_, deviceStatus) = CloseReceipt();
            return deviceStatus;
        }

        public override (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
            var receiptInfo = new ReceiptInfo();

            // Abort all unfinished or erroneus receipts
            AbortReceipt();

            var (fiscalMemorySerialNumber, deviceStatus) = GetFiscalMemorySerialNumber();
            if (!deviceStatus.Ok)
            {
                return (receiptInfo, deviceStatus);
            }

            receiptInfo.FiscalMemorySerialNumber = fiscalMemorySerialNumber;

            // Opening receipt
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

            // Printing receipt's body
            deviceStatus = PrintReceiptBody(receipt);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.AddInfo($"Error occured while printing receipt items");
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

            // Get receipt amount
            decimal? receiptAmount;
            (receiptAmount, deviceStatus) = GetReceiptAmount();
            if (!receiptAmount.HasValue || !deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                return (receiptInfo, deviceStatus);
            }
            receiptInfo.ReceiptAmount = receiptAmount.Value;

            // Closing receipt
            string closeReceiptResponse;
            (closeReceiptResponse, deviceStatus) = CloseReceipt();
            if (!deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                deviceStatus.AddInfo($"Error occurred while closing the receipt");
                return (receiptInfo, deviceStatus);
            }

            // Get receipt number
            string lastDocumentNumberResponse;
            (lastDocumentNumberResponse, deviceStatus) = GetLastDocumentNumber(closeReceiptResponse);
            if (!deviceStatus.Ok)
            {
                (_, deviceStatus) = AbortReceipt();
                deviceStatus.AddInfo($"Error occurred while reading last document number");
                return (receiptInfo, deviceStatus);
            }
            receiptInfo.ReceiptNumber = lastDocumentNumberResponse;

            return (receiptInfo, deviceStatus);
        }

        public override DeviceStatus PrintZReport(Credentials credentials)
        {
            var (response, status) = PrintDailyReport(true);
            System.Diagnostics.Trace.WriteLine("PrintZReport: {0}", response);
            // 0000,0.00,273.60,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00
            return status;
        }

        public override DeviceStatus PrintXReport(Credentials credentials)
        {
            var (response, status) = PrintDailyReport(false);
            System.Diagnostics.Trace.WriteLine("PrintZReport: {0}", response);
            // 0000,0.00,273.60,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00
            return status;
        }
    }
}
