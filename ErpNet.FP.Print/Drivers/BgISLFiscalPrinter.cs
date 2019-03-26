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
            CommandFiscalReceiptSum = 0x33,
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

        public override PrintInfo PrintMoneyDeposit(decimal amount)
        {
            // TODO: status report and error handling

            var (response, _) = Request(CommandMoneyTransfer, amount.ToString("F2", CultureInfo.InvariantCulture));
            Console.WriteLine("PrintMoneyWithdraw: {0}", response);
            return new PrintInfo();
        }

        public override PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            // TODO: status report and error handling

            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("withdraw amount must be positive number");
                ;
            }
            var (response, _) = Request(CommandMoneyTransfer, amount.ToString("F2", CultureInfo.InvariantCulture));
            Console.WriteLine("PrintMoneyWithdraw: {0}", response);
            return new PrintInfo();
        }

        public override PrintInfo PrintReceipt(Receipt receipt)
        {
            // TODO: status report and error handling

            // Receipt header
            var header = string.Format(
                $"{Options["Operator.ID"]},{Options["Operator.Password"].WithMaxLength(Info.OperatorPasswordMaxLength)},{receipt.UniqueSaleNumber}");
            Request(CommandOpenFiscalReceipt, header);

            // Receipt items
            foreach (var item in receipt.Items)
            {
                if (item.IsComment)
                {
                    Request(CommandFiscalReceiptComment, item.Text.WithMaxLength(Info.CommentTextMaxLength));
                }
                else
                {
                    var itemData = new StringBuilder()
                    .Append(item.Text.WithMaxLength(Info.ItemTextMaxLength))
                    .Append('\t').Append(GetTaxGroupText(item.TaxGroup))
                    .Append(item.UnitPrice.ToString("F2", CultureInfo.InvariantCulture));
                    if (item.Quantity != 0)
                    {
                        itemData.Append('*').Append(item.Quantity.ToString(CultureInfo.InvariantCulture));
                    }
                    if (item.Discount != 0)
                    {
                        if (item.IsDiscountPercent)
                        {
                            itemData.Append(',');
                        }
                        else
                        {
                            itemData.Append('$');
                        }
                        itemData.Append(item.Discount.ToString("F2", CultureInfo.InvariantCulture));
                    }
                    Request(CommandFiscalReceiptSale, itemData.ToString());
                }
            }

            // Receipt payments
            if (receipt.Payments == null || receipt.Payments.Count == 0)
            {
                Request(CommandFiscalReceiptTotal);
            }
            else
            {
                //Request(CommandFiscalReceiptSum, "00");
                foreach (var payment in receipt.Payments)
                {
                    var paymentData = new StringBuilder()
                        .Append('\t')
                        .Append(GetPaymentTypeText(payment.PaymentType))
                        .Append(payment.Amount.ToString("F2", CultureInfo.InvariantCulture));
                    Request(CommandFiscalReceiptTotal, paymentData.ToString());
                }
            }


            Request(CommandCloseFiscalReceipt);

            Request(CommandCutThePaper);

            return new PrintInfo();
        }

        public override PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintZeroingReport()
        {
            // TODO: status report and error handling

            var (response, _) = Request(CommandPrintDailyReport);
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

        protected virtual byte[] BuildHostPacket(byte command, byte[] data)
        {
            var packet = new List<byte>();
            packet.Add(MarkerPreamble);
            packet.Add((byte)(MarkerSpace + 4 + (data != null ? data.Length : 0)));
            packet.Add((byte)(MarkerSpace + SequenceNumber));
            packet.Add(command);
            if (data != null)
            {
                packet.AddRange(data);
            }
            packet.Add(MarkerPostamble);
            packet.AddRange(ComputeBCC(packet.Skip(1).ToArray()));
            packet.Add(MarkerTerminator);
            return packet.ToArray();
        }

        protected byte[] RawRequest(byte command, byte[] data)
        {
            SequenceNumber++;
            if (SequenceNumber > MaxSequenceNumber)
            {
                SequenceNumber = 0;
            }
            var request = BuildHostPacket(command, data);
            /*
            Console.WriteLine("Request:");
            foreach (var b in request)
            {
                Console.Write($"{b:X} ");
            }
            Console.WriteLine();
            */
            for (var w = 0; w < MaxWriteRetries; w++)
            {
                // Write packet
                Channel.Write(request);
                // Read response
                var currentFrame = new List<byte>();
                for (var r = 0; r < MaxReadRetries; r++)
                {
                    var buffer = Channel.Read();
                    /*
                    Console.WriteLine("Response:");
                    foreach (var b in buffer)
                    {
                        Console.Write($"{b:X} ");
                    }
                    Console.WriteLine();
                    */
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
                                // By the protocol, it is allowed only one packed response per request.
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
                    // For testing purposes only (view status bits)
                    Console.WriteLine("Status:");
                    int i = 0;
                    foreach (var b in status)
                    {
                        var s = Convert.ToString(b, 2);
                        // Ignore j=0 because bit 7 is reserved
                        for (var j = 1; j < s.Length; j++)
                        {
                            if (s[j] == '1')
                            {
                                Console.Write($"{i}.{7 - j} ");
                            }
                        }
                        i++;
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
