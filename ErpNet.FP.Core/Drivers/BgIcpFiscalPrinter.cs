namespace ErpNet.FP.Core.Drivers.BgIcp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using ErpNet.FP.Core.Configuration;

    /// <summary>
    /// Fiscal printer using the Icp implementation of Isl Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIcpFiscalPrinter" />
    public partial class BgIcpFiscalPrinter : BgFiscalPrinter
    {
        public BgIcpFiscalPrinter(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            IDictionary<string, string>? options = null)
        : base(channel, serviceOptions, options) { }


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

        public override DeviceStatusWithCashAmount Cash(Credentials credentials)
        {
            var (response, status) = Request("F80D");
            var statusEx = new DeviceStatusWithCashAmount(status);
            var amountCash = response.Split(new int[] { 10 });
            if (amountCash.Length != 1)
            {
                statusEx.AddInfo("Error occured while parsing current cash amount");
                statusEx.AddError("E409", "Cannot parse current cash amount");
                return statusEx;
            }

            try
            {
                statusEx.Amount = decimal.Parse(amountCash[0], CultureInfo.InvariantCulture) / 100m;
            }
            catch
            {
                statusEx.AddInfo("Error occured while parsing current cash amount");
                statusEx.AddError("E409", "Cannot parse current cash amount");
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
            var (_, status) = MoneyTransfer(transferAmount.Amount);
            return status;
        }

        public override DeviceStatus PrintMoneyWithdraw(TransferAmount transferAmount)
        {
            var (_, status) = MoneyTransfer(-transferAmount.Amount);
            return status;
        }

        public virtual (ReceiptInfo, DeviceStatus) PrintReceiptBody(Receipt receipt, bool reversalReceipt = false)
        {
            var receiptInfo = new ReceiptInfo();

            var (fiscalMemorySerialNumber, deviceStatus) = GetFiscalMemorySerialNumber();
            if (!deviceStatus.Ok)
            {
                return (receiptInfo, deviceStatus);
            }

            receiptInfo.FiscalMemorySerialNumber = fiscalMemorySerialNumber;

            if (receipt.Items == null || receipt.Items.Count == 0)
            {
                deviceStatus.AddError("E410", "Receipt.Items must be not null or empty");
                return (receiptInfo, deviceStatus);
            }

            uint itemNumber = 0;
            // Receipt items
            foreach (var item in receipt.Items)
            {
                itemNumber++;
                if (item.Type == ItemType.Comment)
                {
                    (_, deviceStatus) = AddComment(receipt.UniqueSaleNumber, item.Text);
                    if (!deviceStatus.Ok)
                    {
                        deviceStatus.AddInfo($"Error occurred in Item {itemNumber}");
                        return (receiptInfo, deviceStatus);
                    }
                }
                else if (item.Type == ItemType.Sale)
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
                            receipt.UniqueSaleNumber,
                            item.Department,
                            item.Text,
                            item.UnitPrice,
                            item.TaxGroup,
                            item.Quantity,
                            item.PriceModifierValue,
                            item.PriceModifierType,
                            reversalReceipt,
                            item.ItemCode);
                    }
                    catch (StandardizedStatusMessageException e)
                    {
                        deviceStatus = new DeviceStatus();
                        deviceStatus.AddError(e.Code, e.Message);
                    }
                    if (!deviceStatus.Ok)
                    {
                        deviceStatus.AddInfo($"Error occurred in Item {itemNumber}");
                        return (receiptInfo, deviceStatus);
                    }
                }
            }

            // Get receipt number and amount
            string receiptNumber;
            decimal? receiptAmount;
            (receiptNumber, receiptAmount, deviceStatus) = GetReceiptNumberAndAmount();
            if (!deviceStatus.Ok)
            {
                return (receiptInfo, deviceStatus);
            }

            receiptInfo.ReceiptNumber = receiptNumber;
            receiptInfo.ReceiptAmount = receiptAmount ?? 0m;

            // Receipt payments
            if (receipt.Payments == null || receipt.Payments.Count == 0)
            {
                deviceStatus = FullPayment();
                if (!deviceStatus.Ok)
                {
                    deviceStatus.AddInfo($"Error occurred while making full payment in cash");
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
                        deviceStatus.AddInfo($"Error occurred in Payment {paymentNumber}");
                        return (receiptInfo, deviceStatus);
                    }
                }
            }

            itemNumber = 0;
            // Receipt items
            foreach (var item in receipt.Items)
            {
                itemNumber++;
                if (item.Type == ItemType.FooterComment)
                {
                    (_, deviceStatus) = AddComment(receipt.UniqueSaleNumber, item.Text);
                    if (!deviceStatus.Ok)
                    {
                        deviceStatus.AddInfo($"Error occurred in Item {itemNumber}");
                        return (receiptInfo, deviceStatus);
                    }
                }
            }

            // Get the receipt date and time (current fiscal device date and time)
            DateTime? dateTime;
            (dateTime, deviceStatus) = GetDateTime();
            if (!deviceStatus.Ok)
            {
                return (receiptInfo, deviceStatus);
            }

            receiptInfo.ReceiptDateTime = dateTime ?? DateTime.Now;

            if (deviceStatus.Ok)
            {
                deviceStatus = FullPayment();
            }

            return (receiptInfo, deviceStatus);
        }

        public override (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt)
        {
            // Printing receipt's body
            var (receiptInfo, deviceStatus) = PrintReceiptBody(receipt);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.AddInfo($"Error occured while printing receipt body");
                return (receiptInfo, deviceStatus);
            }

            return (receiptInfo, deviceStatus);
        }

        public override (ReceiptInfo, DeviceStatus) PrintReversalReceipt(ReversalReceipt reversalReceipt)
        {
            var receiptInfo = new ReceiptInfo();

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
                return (receiptInfo, deviceStatus);
            }

            (receiptInfo, deviceStatus) = PrintReceiptBody(reversalReceipt, true);
            if (!deviceStatus.Ok)
            {
                AbortReceipt();
                deviceStatus.AddInfo($"Error occured while printing reversal receipt body");
                return (receiptInfo, deviceStatus);
            }

            return (receiptInfo, deviceStatus);
        }

        public override DeviceStatus PrintZReport(Credentials credentials)
        {
            var (_, status) = PrintDailyReport(true);
            return status;
        }

        public override DeviceStatus PrintXReport(Credentials credentials)
        {
            var (_, status) = PrintDailyReport(false);
            return status;
        }

        public override DeviceStatus PrintDuplicate(Credentials credentials)
        {
            var (_, status) = Request("AA");
            return status;
        }

        public override DeviceStatusWithRawResponse RawRequest(RequestFrame requestFrame)
        {
            if (requestFrame.RawRequest.Length < 2)
            {
                var deviceStatus = new DeviceStatus();
                deviceStatus.AddError("E401", "Request length must be at least 2 characters");
                return new DeviceStatusWithRawResponse(deviceStatus);
            }
            var (rawResponse, status) = Request(requestFrame.RawRequest);
            var deviceStatusWithRawResponse = new DeviceStatusWithRawResponse(status) { RawResponse = rawResponse };
            return deviceStatusWithRawResponse;
        }

        public override DeviceStatusWithDateTime Reset(Credentials credentials)
        {
            AbortReceipt();
            FullPayment();
            return CheckStatus();
        }
    }
}
