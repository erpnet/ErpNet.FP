using ErpNet.FP.Print.Core;
using System;
using System.Collections.Generic;
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
            CommandFiscalReceiptTotal = 0x35,
            CommandFiscalReceiptComment = 0x36,
            CommandFiscalReceiptSale = 0x31,
            CommandCutThePaper = 0x2d,
            CommandPrintDailyReport = 0x45;
        protected const byte MaxSequenceNumber = 0xFF - MarkerSpace;
        protected const byte MaxWriteRetries = 6;
        protected const byte MaxReadRetries = 20;

        public BgIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null)
        : base(channel, options)
        {
        }

        public override bool IsReady()
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintMoneyDeposit(decimal amount)
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException("amount must be positive number");
                ;
            }
            //Console.WriteLine("PrintMoneyWithdraw: {0}", Request(CommandMoneyTransfer, amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return new PrintInfo();
        }

        public override PrintInfo PrintReceipt(Receipt receipt)
        {
            Console.WriteLine("Print Receipt");
            return new PrintInfo();
        }

        public override PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintZeroingReport()
        {
            Console.WriteLine("PrintZeroingReport: {0}", Request(CommandPrintDailyReport, ""));
            // 0000,0.00,273.60,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00
            return new PrintInfo();
        }

        public override void SetupPrinter()
        {
            // Nothing to be configured for now.
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
            packet.Add((byte)(MarkerSpace + 4 + (byte)data.Length));
            packet.Add((byte)(MarkerSpace + SequenceNumber));
            packet.Add(command);
            packet.AddRange(data);
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
            for (var w = 0; w < MaxWriteRetries; w++)
            {
                // Write packet
                Channel.Write(request);
                // Read response
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
            if (preamblePos + 4 < separatorPos && separatorPos + 6 < postamblePos && postamblePos + 4 < terminatorPos)
            {
                var data = rawResponse.Slice(preamblePos + 4, separatorPos);
                var status = rawResponse.Slice(separatorPos + 1, postamblePos);
                var bcc = rawResponse.Slice(postamblePos + 1, terminatorPos);
                var computedBcc = ComputeBCC(rawResponse.Slice(preamblePos + 1, postamblePos + 1));
                if (bcc.SequenceEqual(computedBcc))
                {
                    return (System.Text.Encoding.UTF8.GetString(data), ParseStatus(status));
                }
            }
            throw new InvalidResponseException("the response is invalid");
        }

        protected (string, DeviceStatus) Request(byte command, string data)
        {
            return ParseResponse(RawRequest(command, System.Text.Encoding.ASCII.GetBytes(data)));
        }

        public (string, DeviceStatus) ReadRawDeviceInfo()
        {
            return Request(CommandGetDeviceInfo, "1");
        }
    }
}
