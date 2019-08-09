using System;
using System.Globalization;
using System.Text;

namespace ErpNet.FP.Core.Drivers.BgIcp
{
    /// <summary>
    /// Fiscal printer using the Icp implementation of Isl Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIcpFiscalPrinter" />
    public partial class BgIcpFiscalPrinter : BgFiscalPrinter
    {
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
                    return "A";
                default:
                    throw new StandardizedStatusMessageException($"Payment type {paymentType} unsupported", "E406");
            }
        }

        public override string GetTaxGroupText(TaxGroup taxGroup)
        {
            switch (taxGroup)
            {
                case TaxGroup.TaxGroup1:
                    return "1";
                case TaxGroup.TaxGroup2:
                    return "2";
                case TaxGroup.TaxGroup3:
                    return "3";
                case TaxGroup.TaxGroup4:
                    return "4";
                case TaxGroup.TaxGroup5:
                    return "5";
                case TaxGroup.TaxGroup6:
                    return "6";
                case TaxGroup.TaxGroup7:
                    return "7";
                case TaxGroup.TaxGroup8:
                    return "8";
                default:
                    throw new StandardizedStatusMessageException($"Tax group {taxGroup} unsupported", "E411");
            }
        }

        public virtual (string, DeviceStatus) SetDeviceDateTime(DateTime dateTime)
        {
            var dateTimeData = new StringBuilder()
                .Append("73")
                .Append(dateTime.ToString("ddMMyyyyHHmmss", CultureInfo.InvariantCulture));
            return Request(dateTimeData.ToString());
        }

        public virtual (string, DeviceStatus) GetRawDeviceInfo()
        {
            var (response00, status00) = Request("00");
            if (!status00.Ok)
            {
                throw new InvalidDeviceInfoException();
            }

            var fields = response00.Split(new int[] { 4, 4 });
            if (fields.Length != 2)
            {
                throw new InvalidDeviceInfoException();
            }
            DeviceNo = PrinterEncoding.GetBytes(fields[1]);

            var (responseFP, statusFP) = Request("F807");
            if (!statusFP.Ok)
            {
                throw new InvalidDeviceInfoException();
            }

            var response = $"{response00}\t{responseFP}";
            return (response, statusFP);
        }

        public virtual (string, DeviceStatus) GetFiscalMemorySerialNumber()
        {
            var (response, status) = Request("F0");
            if (!status.Ok)
            {
                return (response, status);
            }

            var fields = response.Split(new int[] { 8, 8 });
            if (fields.Length != 2)
            {
                status.AddInfo($"Error occured while parsing raw device info");
                status.AddError("E409", $"Wrong format of raw device info");
                return (response, status);
            }
            return (fields[1], status);
        }

        public virtual (string, decimal?, DeviceStatus) GetReceiptNumberAndAmount()
        {
            var (response, status) = Request("F801");
            if (status.Ok)
            {
                var fields = response.Split(new int[] { 6, 8 });
                if (fields.Length == 2)
                {
                    var receiptNumber = fields[0];
                    try
                    { 
                        var receiptAmount = decimal.Parse(fields[1], CultureInfo.InvariantCulture) / 100m;
                        return (receiptNumber, receiptAmount, status);
                    }
                    catch
                    {
                        status.AddInfo("Error occured while parsing current cash amount");
                        status.AddError("E409", "Cannot parse current cash amount");
                    }
                }
                else
                {
                    status.AddInfo($"Error occured while reading receipt number and amount");
                    status.AddError("E409", $"Wrong format of receipt number and amount");
                }
            }

            return (string.Empty, null, status);
        }

        public virtual DeviceStatus FullPayment()
        {
            var (_, status) = Request("4902");
            return status;
        }

        public virtual void AbortReceipt()
        {
            Request("450");
        }

        public virtual (System.DateTime?, DeviceStatus) GetDateTime()
        {
            var (dateTimeResponse, deviceStatus) = Request("F3");
            if (!deviceStatus.Ok)
            {
                deviceStatus.AddInfo($"Error occured while reading current date and time");
                return (null, deviceStatus);
            }


            if (DateTime.TryParseExact(dateTimeResponse,
                "ddMMyyHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime1))
            {
                return (dateTime1, deviceStatus);
            }
            else
            {
                deviceStatus.AddInfo($"Error occured while parsing current date and time");
                deviceStatus.AddError("E409", $"Wrong format of date and time");
                return (null, deviceStatus);
            }
        }

        public virtual (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            var tranferData = new StringBuilder()
                .Append("61")
                .Append(amount > 0 ? '0' : '1') 
                .Append('0') // In cash
                .Append(IcpDecimal(Math.Abs(amount), 12, 2));
            return Request(tranferData.ToString());
        }

        public virtual (string, DeviceStatus) AddItem(
            string uniqueSaleNumber,
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup,
            decimal quantity = 0m,
            decimal priceModifierValue = 0m,
            PriceModifierType priceModifierType = PriceModifierType.None, 
            bool reversalReceipt = false)
        {
            var itemData = new StringBuilder()
                .Append(reversalReceipt ? "24": "44")
                .Append(uniqueSaleNumber)
                .Append(IcpDecimal(quantity, 8, 3))
                .Append(IcpDecimal(999, 8, 0))
                .Append(IcpDecimal(unitPrice, 8, 2))
                .Append("0")
                .Append(GetTaxGroupText(taxGroup))
                .Append("00")
                .Append(itemText.WithMaxLength(Info.ItemTextMaxLength));
            var (response, status) = Request(itemData.ToString());
            if (!status.Ok || priceModifierType == PriceModifierType.None)
            {
                return (response, status);
            }
            var priceModifierIsPercent =
                    priceModifierType == PriceModifierType.DiscountPercent
                    ||
                    priceModifierType == PriceModifierType.SurchargePercent;

            var priceModifierIsDiscount =
                    priceModifierType == PriceModifierType.DiscountPercent
                    ||
                    priceModifierType == PriceModifierType.DiscountAmount;

            var priceModifierData = new StringBuilder()
                .Append(priceModifierIsPercent ? "47" : "46")
                .Append(priceModifierIsDiscount ? "0": "1")
                .Append(IcpDecimal(priceModifierValue, priceModifierIsPercent ? 4 : 8, 2));
            return Request(priceModifierData.ToString());
        }

        public virtual (string, DeviceStatus) AddComment(string uniqueSaleNumber, string text)
        {
            var commentData = new StringBuilder()
                .Append("81")
                .Append(uniqueSaleNumber)
                .Append(text.WithMaxLength(Info.CommentTextMaxLength));
            return Request(commentData.ToString());
        }
        public virtual (string, DeviceStatus) AddPayment(decimal amount, PaymentType paymentType)
        {
            var paymentData = new StringBuilder()
                .Append("49")
                .Append(GetPaymentTypeText(paymentType))
                .Append(IcpDecimal(amount, 12, 2))
                .Append("0");
            return Request(paymentData.ToString());
        }

        public override string GetReversalReasonText(ReversalReason reversalReason)
        {
            switch (reversalReason)
            {
                case ReversalReason.OperatorError:
                    return "0";
                case ReversalReason.Refund:
                    return "1";
                case ReversalReason.TaxBaseReduction:
                    return "2";
                default:
                    return "0";
            }
        }

        public virtual (string, DeviceStatus) OpenReversalReceipt(
            ReversalReason reason,
            string receiptNumber,
            System.DateTime receiptDateTime,
            string fiscalMemorySerialNumber,
            string uniqueSaleNumber,
            string operatorId,
            string operatorPassword)
        {
            var reversalReceiptData = new StringBuilder()
                .Append("20")
                .Append(GetReversalReasonText(reason))
                .Append("0000")
                .Append(receiptNumber)
                .Append(receiptDateTime.ToString("ddMMyyyyHHmmss", CultureInfo.InvariantCulture))
                .Append(fiscalMemorySerialNumber)
                .Append(uniqueSaleNumber);
            return Request(reversalReceiptData.ToString());
        }

        public virtual (string, DeviceStatus) PrintDailyReport(bool zeroing = true)
        {
            return Request(zeroing ? "510" : "511");
        }

        protected static string IcpDecimal(decimal value, int length = 10, int digitsAfterPoint = 2)
        {
            return ((int)Math.Round(value * 10.IntPow(digitsAfterPoint), 0, MidpointRounding.AwayFromZero)).ToString($"D{length}");
        }

        // 6 Bytes x 8 bits
        protected static readonly (string?, string, StatusMessageType)[] StatusBitsStrings = new (string?, string, StatusMessageType)[] {
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "Fiscal receipt is open before power down", StatusMessageType.Info),
            ("E302", "Printer cover open", StatusMessageType.Error),
            ("E301", "End of paper", StatusMessageType.Error),
            ("E301", "End of paper (additional sensor)", StatusMessageType.Error),
            ("E199", "Operation error", StatusMessageType.Error),
            ("E299", "FM general error", StatusMessageType.Error),
            ("E199", "General error", StatusMessageType.Error),

            (null, "Discal receipt is in invoice mode", StatusMessageType.Info),
            (null, "Partial payment started", StatusMessageType.Info),
            (null, "Pending payments", StatusMessageType.Info),
            (null, "Pending daily report", StatusMessageType.Info),
            (null, "Invalid value of command parameter", StatusMessageType.Info),
            (null, "Daily registers overflow", StatusMessageType.Info),
            (null, "Overflow during command execution", StatusMessageType.Info),
            ("E206", "EJ is full", StatusMessageType.Error),

            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "Contract with the Service Company is expired", StatusMessageType.Info),
            (null, "Printer is in MENU mode", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "Fiscal receipt is open", StatusMessageType.Info),
            (null, "Invoice numbers are initailized", StatusMessageType.Info),

            ("E201", "DataFlash error", StatusMessageType.Error),
            ("E201", "EJ error", StatusMessageType.Error),
            ("E202", "FM error", StatusMessageType.Error),
            (null, "Device is fiscalized", StatusMessageType.Info),
            ("W201", "There is space for less then 50 records in FM", StatusMessageType.Warning),
            ("E201", "FM full", StatusMessageType.Error),
            (null, "RTC is not found", StatusMessageType.Info),
            (null, "RAM is reset", StatusMessageType.Info),

            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "Clock is in Summer Time", StatusMessageType.Info),

            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved)
        };

    }
}
