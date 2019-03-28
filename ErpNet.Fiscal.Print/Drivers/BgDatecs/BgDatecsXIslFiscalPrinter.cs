using ErpNet.Fiscal.Print.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ErpNet.Fiscal.Print.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.Fiscal.Drivers.BgIslFiscalPrinter" />
    public class BgDatecsXIslFiscalPrinter : BgIslFiscalPrinter
    {
        public BgDatecsXIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }

        public override IDictionary<string, string> GetDefaultOptions()
        {
            return new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Password"] = "0000",

                ["Administrator.ID"] = "20",
                ["Administrator.Password"] = "9999"
            };
        }

        protected override DeviceStatus ParseStatus(byte[] status)
        {
            // TODO: Device status parser
            return new DeviceStatus();
        }

        public override (string, DeviceStatus) PrintDailyReport()
        {
            return Request(CommandPrintDailyReport, "Z\t");
        }

        public override (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            return Request(CommandMoneyTransfer, string.Join("\t", new string[] {
                amount >= 0 ? "0" : "1",
                Math.Abs(amount).ToString("F2", CultureInfo.InvariantCulture),
                ""
                }));
        }


        public override (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber, string operatorID, string operatorPassword)
        {
            // Protocol: {OpCode}<SEP>{OpPwd}<SEP>{NSale}<SEP>{TillNmb}<SEP>{Invoice}<SEP>
            var header = string.Join("\t",
                new string[] {
                    operatorID,
                    operatorPassword.WithMaxLength(Info.OperatorPasswordMaxLength),
                    uniqueSaleNumber,
                    "1",
                    "",
                    ""
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public override string GetTaxGroupText(TaxGroup taxGroup)
        {

            switch (taxGroup)
            {
                case TaxGroup.GroupA:
                    return "1";
                case TaxGroup.GroupB:
                    return "2";
                case TaxGroup.GroupC:
                    return "3";
                case TaxGroup.GroupD:
                    return "4";
                default:
                    return "2";
            }
        }

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

        public override (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup = TaxGroup.GroupB,
            decimal quantity = 0,
            decimal discount = 0,
            bool isDiscountPercent = true)
        {
            // Protocol: {PluName}<SEP>{TaxCd}<SEP>{Price}<SEP>{Quantity}<SEP>{DiscountType}<SEP>{DiscountValue}<SEP>{Department}<SEP>
            var itemData = string.Join("\t",
                itemText.WithMaxLength(Info.ItemTextMaxLength),
                GetTaxGroupText(taxGroup),
                unitPrice.ToString("F2", CultureInfo.InvariantCulture),
                quantity.ToString(CultureInfo.InvariantCulture),
                discount == 0 ? "0" : (isDiscountPercent ? (discount >= 0 ? "1" : "2") : (discount >= 0 ? "3" : "4")),
                discount.ToString("F2", CultureInfo.InvariantCulture),
                "0",
                "");
            return Request(CommandFiscalReceiptSale, itemData);
        }

        public override (string, DeviceStatus) AddComment(string text)
        {
            return Request(CommandFiscalReceiptComment, text.WithMaxLength(Info.CommentTextMaxLength) + "\t");
        }
        public override (string, DeviceStatus) AddPayment(decimal amount, PaymentType paymentType = PaymentType.Cash)
        {
            // Protocol: {PaidMode}<SEP>{Amount}<SEP>{Type}<SEP>
            var paymentData = string.Join("\t",
                GetPaymentTypeText(paymentType),
                amount.ToString("F2", CultureInfo.InvariantCulture),
                "1",
                "");

            return Request(CommandFiscalReceiptTotal, paymentData);
        }

        protected override byte[] BuildHostFrame(byte command, byte[] data)
        {
            // Frame header
            var frame = new List<byte>();
            frame.Add(MarkerPreamble);
            frame.AddRange(UInt16To4Bytes((UInt16)(MarkerSpace + 10 + (data != null ? data.Length : 0))));
            frame.Add((byte)(MarkerSpace + FrameSequenceNumber));
            frame.AddRange(UInt16To4Bytes((UInt16)command));

            // Frame data
            if (data != null)
            {
                frame.AddRange(data);
            }

            // Frame footer
            frame.Add(MarkerPostamble);
            frame.AddRange(ComputeBCC(frame.Skip(1).ToArray()));
            frame.Add(MarkerTerminator);

            return frame.ToArray();
        }

        protected override (string, DeviceStatus) ParseResponse(byte[] rawResponse)
        {
            if (rawResponse == null)
            {
                throw new InvalidResponseException("no response");
            }
            var (preamblePos, separatorPos, postamblePos, terminatorPos) = (0u, 0u, 0u, 0u);
            for (var i = 0u; i < rawResponse.Length; i++)
            {
                var b = rawResponse[i];
                switch (b)
                {
                    case MarkerPreamble:
                        preamblePos = i;
                        break;
                    case MarkerSeparator:
                        separatorPos = i;
                        break;
                    case MarkerPostamble:
                        postamblePos = i;
                        break;
                    case MarkerTerminator:
                        terminatorPos = i;
                        break;
                }
            }
            if (preamblePos + 10 <= separatorPos && separatorPos + 8 < postamblePos && postamblePos + 4 < terminatorPos)
            {
                var data = rawResponse.Slice(preamblePos + 10, separatorPos);
                var status = rawResponse.Slice(separatorPos + 1, postamblePos);
                var bcc = rawResponse.Slice(postamblePos + 1, terminatorPos);
                var computedBcc = ComputeBCC(rawResponse.Slice(preamblePos + 1, postamblePos + 1));
                if (bcc.SequenceEqual(computedBcc))
                {
                    // For debugging purposes only (to view status bits)    
                    var deviceID = (Info == null ? "" : Info.SerialNumber);
                    System.Diagnostics.Debug.WriteLine($"Status of device {deviceID}");
                    for (var i = 0; i < status.Length; i++)
                    {
                        byte mask = 0b10000000;
                        byte b = status[i];
                        // Ignore j==0 because bit 7 is always reserved and 1
                        for (var j = 1; j < 8; j++)
                        {
                            mask >>= 1;
                            if ((mask & b) == mask)
                            {
                                System.Diagnostics.Debug.Write($"{i}.{7 - j} ");
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("");

                    var response = Encoding.UTF8.GetString(data);
                    System.Diagnostics.Debug.WriteLine($"Response: {response}");

                    return (response, ParseStatus(status));
                }
            }
            throw new InvalidResponseException("the response is invalid");
        }

    }
}
