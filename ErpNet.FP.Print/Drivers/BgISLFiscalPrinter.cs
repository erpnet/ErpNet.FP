using ErpNet.FP.Print.Core;
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ErpNet.FP.Print.Drivers
{
    /// <summary>
    /// Fiscal printer using the ISL implementation.
    /// </summary>
    /// <seealso cref="ErpNet.FP.BgFiscalPrinter" />
    public class BgIslFiscalPrinter : BgFiscalPrinter
    {
        protected byte SequenceNumber = 0;
        protected const byte
            MarkerSpace = 0x20,
            MarkerSyn = 0x16,
            MarkerNak = 0x15,
            MarkerPreamble = 0x01,
            MarkerPostamble = 0x05,
            MarkerSeparator = 0x04,
            MarkerTerminator = 0x03;
        protected const byte
            DigitZero = 0x30,
            DigitOne = 0x31;
        protected const byte
            CommandGetStatus = 0x4a,
            CommandGetDeviceInfo = 0x5a,
            CommandMoneyTransfer = 0x46,
            CommandOpenFiscalReceipt = 0x30,
            CommandCloseFiscalReceipt = 0x38,
            CommandAbortFiscalReceipt = 0x82,
            CommandFiscalReceiptTotal = 0x35,
            CommandFiscalReceiptComment = 0x36,
            CommandFiscalReceiptSale = 0x31,
            CommandCutThePaper = 0x2d,
            CommandPrintDailyReport = 0x45;
        protected const byte MaxSequenceNumber = 0xFF - MarkerSpace;
        protected const byte MaxWriteRetries = 6;
        protected const byte MaxReadRetries = 200;

        public BgIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }

        public override bool IsReady()
        {
            // TODO: status report and error handling

            var (response, _) = Request(CommandGetStatus);
            Console.WriteLine("IsReady: {0}", response);
            return true;
        }

        public virtual (string, DeviceStatus) MoneyTransfer(decimal amount)
        {
            return Request(CommandMoneyTransfer, amount.ToString("F2", CultureInfo.InvariantCulture));
        }

        public override PrintInfo PrintMoneyDeposit(decimal amount)
        {
            // TODO: status report and error handling

            var (response, _) = MoneyTransfer(amount);
            Console.WriteLine("PrintMoneyWithdraw: {0}", response);
            return new PrintInfo();
        }

        public override PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            // TODO: status report and error handling

            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("withdraw amount must be positive number");
            }
            var (response, _) = MoneyTransfer(amount);
            Console.WriteLine("PrintMoneyWithdraw: {0}", response);
            return new PrintInfo();
        }

        public virtual (string, DeviceStatus) OpenReceipt(string uniqueSaleNumber, string operatorID, string operatorPassword)
        {
            var header = string.Join(",",
                new string[] {
                    operatorID,
                    operatorPassword.WithMaxLength(Info.OperatorPasswordMaxLength),
                    uniqueSaleNumber
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public virtual (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup = TaxGroup.GroupB,
            decimal quantity = 0,
            decimal discount = 0,
            bool isDiscountPercent = true)
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
            if (discount != 0)
            {
                itemData
                    .Append(isDiscountPercent ? ',' : '$')
                    .Append(discount.ToString("F2", CultureInfo.InvariantCulture));
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

        public virtual (string, DeviceStatus) CutThePaper()
        {
            return Request(CommandCutThePaper);
        }

        public virtual (string, DeviceStatus) FullPayment()
        {
            return Request(CommandFiscalReceiptTotal);
        }

        public virtual (string, DeviceStatus) AddPayment(decimal amount, PaymentType paymentType = PaymentType.Cash)
        {
            var paymentData = new StringBuilder()
                .Append('\t')
                .Append(GetPaymentTypeText(paymentType))
                .Append(amount.ToString("F2", CultureInfo.InvariantCulture));
            return Request(CommandFiscalReceiptTotal, paymentData.ToString());
        }

        public override PrintInfo PrintReceipt(Receipt receipt)
        {
            // TODO: status report and error handling

            // Receipt header
            OpenReceipt(receipt.UniqueSaleNumber, Options["Operator.ID"], Options["Operator.Password"]);

            // Receipt items
            foreach (var item in receipt.Items)
            {
                if (item.IsComment)
                {
                    AddComment(item.Text);
                }
                else
                {
                    AddItem(item.Text, item.UnitPrice, item.TaxGroup, item.Quantity, item.Discount, item.IsDiscountPercent);
                }
            }

            // Receipt payments
            if (receipt.Payments == null || receipt.Payments.Count == 0)
            {
                FullPayment();
            }
            else
            {
                foreach (var payment in receipt.Payments)
                {
                    AddPayment(payment.Amount, payment.PaymentType);
                }
            }

            // Receipt finalization
            CloseReceipt();
            CutThePaper();

            return new PrintInfo();
        }

        public override PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public virtual (string, DeviceStatus) PrintDailyReport()
        {
            return Request(CommandPrintDailyReport);
        }

        public override PrintInfo PrintZeroingReport()
        {
            // TODO: status report and error handling

            var (response, _) = PrintDailyReport();
            Console.WriteLine("PrintZeroingReport: {0}", response);
            // 0000,0.00,273.60,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00
            return new PrintInfo();
        }

        protected virtual byte[] ComputeBCC(byte[] fragment)
        {
            UInt16 bccSum = 0;
            foreach (byte b in fragment)
            {
                bccSum += b;
            }
            return new byte[]{
                (byte)((bccSum >> 12 & 0x0f) + DigitZero),
                (byte)((bccSum >> 8 & 0x0f) + DigitZero),
                (byte)((bccSum >> 4 & 0x0f) + DigitZero),
                (byte)((bccSum >> 0 & 0x0f) + DigitZero)
            };
        }

        protected virtual byte[] BuildHostFrame(byte command, byte[] data)
        {
            // Frame header
            var frame = new List<byte>
            {
                MarkerPreamble,
                (byte)(MarkerSpace + 4 + (data != null ? data.Length : 0)),
                (byte)(MarkerSpace + SequenceNumber),
                command
            };

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

        protected byte[] RawRequest(byte command, byte[] data)
        {
            SequenceNumber++;
            if (SequenceNumber > MaxSequenceNumber)
            {
                SequenceNumber = 0;
            }
            var request = BuildHostFrame(command, data);
            for (var w = 0; w < MaxWriteRetries; w++)
            {
                // Write request frame
                Channel.Write(request);

                // Read response frames
                var currentFrame = new List<byte>();
                for (var r = 0; r < MaxReadRetries; r++)
                {
                    var buffer = Channel.Read();
                    var readFrames = new List<List<byte>>();
                    foreach (var b in buffer)
                    {
                        currentFrame.Add(b);
                        // Split buffer by following separators
                        if (b == MarkerNak || b == MarkerSyn || b == MarkerTerminator)
                        {
                            readFrames.Add(currentFrame);
                            currentFrame = new List<byte>();
                        }
                    }
                    var (wait, repeat) = (false, false);
                    foreach (var frame in readFrames)
                    {
                        switch (frame[0])
                        {
                            case MarkerNak:
                                // Only last non-packed frame matters if there are many readed
                                // So change the state accordingly
                                (wait, repeat) = (false, true);
                                break;
                            case MarkerSyn:
                                // Only last non-packed frame matters if there are many readed
                                // So change the state accordingly
                                (wait, repeat) = (true, false);
                                break;
                            case MarkerPreamble:
                                // By the protocol, it is allowed only one packed frame response per request.
                                // So return first occurence of packed frame as response.
                                return frame.ToArray();
                        }
                    }
                    if (wait)
                    {
                        // The FiscalPrinter is still not ready, so make another read
                        continue;
                    }
                    if (repeat)
                    {
                        // The FiscalPrinter cannot answer, so make the request again
                        break;
                    }
                }
            }
            return null;
        }

        protected (string, DeviceStatus) ParseResponse(byte[] rawResponse)
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
            if (preamblePos + 4 <= separatorPos && separatorPos + 6 < postamblePos && postamblePos + 4 < terminatorPos)
            {
                var data = rawResponse.Slice(preamblePos + 4, separatorPos);
                var status = rawResponse.Slice(separatorPos + 1, postamblePos);
                var bcc = rawResponse.Slice(postamblePos + 1, terminatorPos);
                var computedBcc = ComputeBCC(rawResponse.Slice(preamblePos + 1, postamblePos + 1));
                if (bcc.SequenceEqual(computedBcc))
                {
                    // For debugging purposes only (to view status bits)    
                    var deviceID = (Info == null ? "" : Info.SerialNumber);
                    Console.WriteLine($"Status of device {deviceID}");
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
                                Console.Write($"{i}.{7 - j} ");
                            }
                        }
                    }
                    Console.WriteLine();

                    return (Encoding.UTF8.GetString(data), ParseStatus(status));
                }
            }
            throw new InvalidResponseException("the response is invalid");
        }

        protected (string, DeviceStatus) Request(byte command, string data)
        {
            Console.WriteLine($"Request({command:X}): '{data}'");
            return ParseResponse(RawRequest(command, PrinterEncoding.GetBytes(data)));
        }

        protected (string, DeviceStatus) Request(byte command)
        {
            Console.WriteLine($"Request({command:X})");
            return ParseResponse(RawRequest(command, null));
        }

        public (string, DeviceStatus) GetRawDeviceInfo()
        {
            return Request(CommandGetDeviceInfo, "1");
        }

    }
}
