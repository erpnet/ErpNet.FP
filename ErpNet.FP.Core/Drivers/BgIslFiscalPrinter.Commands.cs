using System;
using System.Globalization;
using System.Text;

namespace ErpNet.FP.Core.Drivers
{
    public abstract partial class BgIslFiscalPrinter : BgFiscalPrinter
    {
        protected const byte
            CommandGetStatus = 0x4a,
            CommandGetDeviceInfo = 0x5a,
            CommandMoneyTransfer = 0x46,
            CommandOpenFiscalReceipt = 0x30,
            CommandCloseFiscalReceipt = 0x38,
            CommandAbortFiscalReceipt = 0x3c,
            CommandFiscalReceiptTotal = 0x35,
            CommandFiscalReceiptComment = 0x36,
            CommandFiscalReceiptSale = 0x31,
            CommandPrintDailyReport = 0x45,
            CommandGetDateTime = 0x3e,
            CommandGetReceiptStatus = 0x4c,
            CommandGetLastDocumentNumber = 0x71,
            CommandReadLastReceiptQRCodeData = 0x74;

        public override string GetReversalReasonText(ReversalReason reversalReason)
        {
            switch (reversalReason)
            {
                case ReversalReason.OperatorError:
                    return "1";
                case ReversalReason.Refund:
                    return "0";
                case ReversalReason.TaxBaseReduction:
                    return "2";
                default:
                    return "1";
            }
        }

        public virtual (string, DeviceStatus) GetStatus()
        {
            return Request(CommandGetStatus);
        }

        public virtual (string, DeviceStatus) GetLastDocumentNumber(string closeReceiptResponse)
        {
            return Request(CommandGetLastDocumentNumber);
        }

        public virtual (decimal?, DeviceStatus) GetReceiptAmount()
        {
            decimal? receiptAmount = null;

            var (receiptStatusResponse, deviceStatus) = Request(CommandGetReceiptStatus, "T");
            if (!deviceStatus.Ok)
            {
                deviceStatus.Statuses.Add($"Error occured while reading last receipt status");
                return (null, deviceStatus);
            }

            var fields = receiptStatusResponse.Split(',');
            if (fields.Length < 3)
            {
                deviceStatus.Statuses.Add($"Error occured while parsing last receipt status");
                deviceStatus.Errors.Add("Wrong format of receipt status");
                return (null, deviceStatus);
            }

            try
            {
                var amountString = fields[2];
                if (amountString.Length > 0)
                {
                    switch (amountString[0])
                    {
                        case '+':
                            receiptAmount = decimal.Parse(amountString.Substring(1), System.Globalization.CultureInfo.InvariantCulture) / 100m;
                            break;
                        case '-':
                            receiptAmount = -decimal.Parse(amountString.Substring(1), System.Globalization.CultureInfo.InvariantCulture) / 100m;
                            break;
                        default:
                            receiptAmount = decimal.Parse(amountString, System.Globalization.CultureInfo.InvariantCulture);
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                deviceStatus = new DeviceStatus();
                deviceStatus.Statuses.Add($"Error occured while parsing the amount of last receipt status");
                deviceStatus.Errors.Add(e.Message);
                return (null, deviceStatus);
            }

            return (receiptAmount, deviceStatus);
        }


        public virtual (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            return Request(CommandMoneyTransfer, amount.ToString("F2", CultureInfo.InvariantCulture));
        }

        public virtual (System.DateTime?, DeviceStatus) GetDateTime()
        {
            var (dateTimeResponse, deviceStatus) = Request(CommandGetDateTime);
            if (!deviceStatus.Ok)
            {
                deviceStatus.Statuses.Add($"Error occured while reading current date and time");
                return (null, deviceStatus);
            }

            try
            {
                var dateTime = DateTime.ParseExact(dateTimeResponse,
                    "dd-MM-yy HH:mm:ss",
                    CultureInfo.InvariantCulture);
                return (dateTime, deviceStatus);
            }
            catch
            {
                deviceStatus.Statuses.Add($"Error occured while parsing current date and time");
                deviceStatus.Errors.Add($"Wrong format of date and time");
                return (null, deviceStatus);
            }
        }

        public virtual (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber)
        {
            var header = string.Join(",",
                new string[] {
                    Options.ValueOrDefault("Operator.ID", "1"),
                    Options.ValueOrDefault("Operator.Password", "0000").WithMaxLength(Info.OperatorPasswordMaxLength),
                    uniqueSaleNumber
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public virtual (string, DeviceStatus) OpenReversalReceipt(
            ReversalReason reason,
            string receiptNumber,
            System.DateTime receiptDateTime,
            string fiscalMemorySerialNumber,
            string uniqueSaleNumber)
        {
            // Protocol: {ClerkNum},{Password},{UnicSaleNum}[{Tab}{Refund}{Reason},{DocLink},{DocLinkDT}{Tab}{FiskMem}
            var headerData = new StringBuilder()
                .Append(Options.ValueOrDefault("Administrator.ID", "20"))
                .Append(',')
                .Append(Options.ValueOrDefault("Administrator.Password", "9999").WithMaxLength(Info.OperatorPasswordMaxLength))
                .Append(',')
                .Append(uniqueSaleNumber)
                .Append('\t')
                .Append('R')
                .Append(GetReversalReasonText(reason))
                .Append(',')
                .Append(receiptNumber)
                .Append(',')
                .Append(receiptDateTime.ToString("dd-MM-yy HH:mm:ss", CultureInfo.InvariantCulture))
                .Append('\t')
                .Append(fiscalMemorySerialNumber);

            return Request(CommandOpenFiscalReceipt, headerData.ToString());
        }

        public virtual (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup,
            decimal quantity = 0,
            decimal priceModifierValue = 0,
            PriceModifierType priceModifierType = PriceModifierType.None)
        {
            var itemData = new StringBuilder()
                .Append(itemText.WithMaxLength(Info.ItemTextMaxLength))
                .Append('\t').Append(GetTaxGroupText(taxGroup))
                .Append(unitPrice.ToString("F2", CultureInfo.InvariantCulture));
            if (quantity != 0)
            {
                itemData
                    .Append('*')
                    .Append(quantity.ToString(CultureInfo.InvariantCulture));
            }
            if (priceModifierType != PriceModifierType.None)
            {
                itemData
                    .Append(
                        priceModifierType == PriceModifierType.DiscountPercent
                        ||
                        priceModifierType == PriceModifierType.SurchargePercent
                        ? ',' : '$')
                    .Append((
                        priceModifierType == PriceModifierType.DiscountPercent
                        ||
                        priceModifierType == PriceModifierType.DiscountAmount
                        ? -priceModifierValue : priceModifierValue).ToString("F2", CultureInfo.InvariantCulture));
            }
            return Request(CommandFiscalReceiptSale, itemData.ToString());
        }

        public virtual (string, DeviceStatus) AddComment(string text)
        {
            return Request(CommandFiscalReceiptComment, text.WithMaxLength(Info.CommentTextMaxLength));
        }

        public virtual (string, DeviceStatus) CloseReceipt()
        {
            return Request(CommandCloseFiscalReceipt);
        }

        public virtual (string, DeviceStatus) AbortReceipt()
        {
            return Request(CommandAbortFiscalReceipt);
        }

        public virtual (string, DeviceStatus) FullPayment()
        {
            return Request(CommandFiscalReceiptTotal);
        }

        public virtual (string, DeviceStatus) AddPayment(decimal amount, PaymentType paymentType)
        {
            var paymentData = new StringBuilder()
                .Append('\t')
                .Append(GetPaymentTypeText(paymentType))
                .Append(amount.ToString("F2", CultureInfo.InvariantCulture));
            return Request(CommandFiscalReceiptTotal, paymentData.ToString());
        }

        public virtual (string, DeviceStatus) PrintDailyReport()
        {
            return Request(CommandPrintDailyReport);
        }

        public virtual (string, DeviceStatus) GetLastReceiptQRCodeData()
        {
            return Request(CommandReadLastReceiptQRCodeData);
        }

        public virtual (string, DeviceStatus) GetRawDeviceInfo()
        {
            return Request(CommandGetDeviceInfo, "1");
        }
    }
}
