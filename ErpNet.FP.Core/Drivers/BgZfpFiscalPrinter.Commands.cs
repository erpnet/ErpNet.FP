using System.Text;
using ErpNet.FP.Core;
using System.Globalization;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Drivers
{
    public abstract partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        protected const byte
            CommandReadFDNumbers = 0x60,
            CommandGetStatus = 0x20,
            CommandVersion = 0x21,
            CommandPrintDailyFiscalReport = 0x7c,
            CommandNoFiscalRAorPOAmount = 0x3b,
            CommandOpenReceipt = 0x30,
            CommandCloseReceipt = 0x38,
            CommandFullPaymentAndCloseReceipt = 0x36,
            CommandAbortReceipt = 0x39,
            CommandSellCorrection = 0x31,
            CommandFreeText = 0x37,
            CommandPayment = 0x35,
            CommandReadLastReceiptQRCodeData = 0x72,
            CommandGSCommand = 0x1d;
        protected const byte
            // Protocol: 36 symbols for article's name. 34 symbols are printed on paper.
            // Attention: ItemText should be padded right with spaces until reaches mandatory 
            // length of 36 symbols. Otherwise we will have syntax error!
            ItemTextMandatoryLength = 36;

        public virtual (string, DeviceStatus) GetStatus()
        {
            var (deviceStatus, _ /* ignore commandStatus */) = ParseResponseAsByteArray(RawRequest(CommandGetStatus, null));
            return ("", ParseStatus(deviceStatus));
        }

        public virtual (string, DeviceStatus) GetLastReceiptQRCodeData()
        {
            return Request(CommandReadLastReceiptQRCodeData, "B");
        }

        public virtual (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            return Request(CommandNoFiscalRAorPOAmount, string.Join(";", new string[]
            {
                Options.ValueOrDefault("Operator.ID", "1"),
                Options.ValueOrDefault("Operator.Password", "0000"),
                "0", // Protocol: Reserved 
                amount.ToString("F2", CultureInfo.InvariantCulture)
            }));
        }

        public virtual (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber)
        {
            // Protocol: <OperNum[1..2]> <;> <OperPass[6]> <;> <ReceiptFormat[1]> <;> 
            //           <PrintVAT[1]> <;> <FiscalRcpPrintType[1]> {<’$’> <UniqueReceiptNumber[24]>}
            return Request(CommandOpenReceipt, string.Join(";", new string[] {
                Options.ValueOrDefault("Operator.ID", "1"),
                Options.ValueOrDefault("Operator.Password", "0000"),
                "1", // Protocol: Detailed
                "1", // Protocol: Include VAT
                "4$"+uniqueSaleNumber, // Protocol: Buffered printing (faster), delimiter '$' before USN
            }));
        }

        public virtual (string, DeviceStatus) OpenReversalReceipt(
            ReversalReason reason,
            string receiptNumber,
            System.DateTime receiptDateTime,
            string fiscalMemorySerialNumber,
            string uniqueSaleNumber)
        {
            // Protocol: <OperNum[1..2]> <;> <OperPass[6]> <;> <ReceiptFormat[1]> <;>
            //            < PrintVAT[1] > <;> < StornoRcpPrintType[1] > <;> < StornoReason[1] > <;>
            //            < RelatedToRcpNum[1..6] > <;> < RelatedToRcpDateTime ”DD-MM-YY HH:MM”> <;>
            //            < FMNum[8] > {<;> < RelatedToURN[24] >}            
            return Request(CommandOpenReceipt, string.Join(";", new string[] {
                Options.ValueOrDefault("Operator.ID", "1"),
                Options.ValueOrDefault("Operator.Password", "0000"),
                "1", // Protocol: Detailed
                "1", // Protocol: Include VAT
                "D", // Protocol: Buffered printing
                GetReversalReasonText(reason),
                receiptNumber,
                receiptDateTime.ToString("dd-MM-yy HH:mm", CultureInfo.InvariantCulture),
                fiscalMemorySerialNumber,
                uniqueSaleNumber
            }));
        }

        public virtual (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup = TaxGroup.GroupB,
            decimal quantity = 0,
            decimal priceModifierValue = 0,
            PriceModifierType priceModifierType = PriceModifierType.None)
        {
            // Protocol: <NamePLU[36]><;><OptionVATClass[1]><;><Price[1..10]>{<'*'>< Quantity[1..10]>}
            //           {<','><DiscAddP[1..7]>}{<':'><DiscAddV[1..8]>}
            var itemData = new StringBuilder()
                .Append(itemText.WithMaxLength(Info.ItemTextMaxLength).PadRight(ItemTextMandatoryLength))
                .Append(';')
                .Append(GetTaxGroupText(taxGroup))
                .Append(';')
                .Append(unitPrice.ToString("F2", CultureInfo.InvariantCulture));
            if (quantity != 0)
            {
                itemData
                    .Append('*')
                    .Append(quantity.ToString(CultureInfo.InvariantCulture));
            }
            switch (priceModifierType)
            {
                case PriceModifierType.DiscountPercent:
                    itemData
                        .Append(',')
                        .Append((-priceModifierValue).ToString("F2", CultureInfo.InvariantCulture));
                    break;
                case PriceModifierType.DiscountAmount:
                    itemData
                        .Append(':')
                        .Append((-priceModifierValue).ToString("F2", CultureInfo.InvariantCulture));
                    break;
                case PriceModifierType.SurchargePercent:
                    itemData
                        .Append(',')
                        .Append(priceModifierValue.ToString("F2", CultureInfo.InvariantCulture));
                    break;
                case PriceModifierType.SurchargeAmount:
                    itemData
                        .Append(':')
                        .Append(priceModifierValue.ToString("F2", CultureInfo.InvariantCulture));
                    break;
                default:
                    break;
            }
            return Request(CommandSellCorrection, itemData.ToString());
        }

        public virtual (string, DeviceStatus) AddComment(string text)
        {
            return Request(CommandFreeText, text.WithMaxLength(Info.CommentTextMaxLength));
        }

        public virtual (string, DeviceStatus) CloseReceipt()
        {
            return Request(CommandCloseReceipt);
        }

        public virtual (string, DeviceStatus) AbortReceipt()
        {
            return Request(CommandAbortReceipt);
        }

        public virtual (string, DeviceStatus) FullPaymentAndCloseReceipt()
        {
            return Request(CommandFullPaymentAndCloseReceipt);
        }

        public virtual (string, DeviceStatus) AddPayment(
            decimal amount,
            PaymentType paymentType = PaymentType.Cash)
        {
            // Protocol: input: <PaymentType [1..2]> <;> <OptionChange [1]> <;> <Amount[1..10]> {<;><OptionChangeType[1]>}
            return Request(CommandPayment, string.Join(";", new string[] {
                GetPaymentTypeText(paymentType),
                "1", // Procotol: Without change
                amount.ToString("F2", CultureInfo.InvariantCulture)+"*"
            }));
        }

        public virtual (string, DeviceStatus) PrintDailyReport()
        {
            return Request(CommandPrintDailyFiscalReport, "Z");
        }

        public virtual (string, DeviceStatus) GetRawDeviceInfo()
        {
            var (responseFD, _) = Request(CommandReadFDNumbers);
            var (responseV, deviceStatus) = Request(CommandVersion);
            return (responseV + responseFD, deviceStatus);
        }
    }
}
