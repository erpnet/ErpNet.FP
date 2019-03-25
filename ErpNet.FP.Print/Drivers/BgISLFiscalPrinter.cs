using System;
using System.Linq;
using System.Collections.Generic;
using ErpNet.FP.Print.Core;

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
            CommandGetDeviceInfo = 0x5a;
        protected const byte MaxSequenceNumber = 0xFF - MarkerSpace;
        protected const byte MaxWriteRetries = 6;

        public BgIslFiscalPrinter(IChannel channel, IDictionary<string, string> options = null) 
        : base (channel, options) {
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
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintReceipt(Receipt receipt)
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new System.NotImplementedException();
        }

        public override PrintInfo PrintZeroingReport()
        {
            throw new System.NotImplementedException();
        }

        public override void SetupPrinter()
        {
            // Nothing to be configured for now.
        }

        protected virtual byte[] ComputeBCC(byte[] fragment) {
            UInt16 bccSum = 0;
            foreach(byte b in fragment) {
                bccSum += b;
            }
            return new byte[]{
                (byte)(((byte)(bccSum >> 0x0c) & 0x0f) + DigitZero), 
                (byte)(((byte)(bccSum >> 0x08) & 0x0f) + DigitZero), 
                (byte)(((byte)(bccSum >> 0x04) & 0x0f) + DigitZero), 
                (byte)(((byte)(bccSum >> 0x00) & 0x0f) + DigitZero)
            };
        }

        protected virtual byte[] BuildHostPacket(byte command, byte[] data) {
            var dataLength = data.Length;
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

        protected byte[] RawRequest(byte command, byte[] data) {
            SequenceNumber++;
            if (SequenceNumber > MaxSequenceNumber) {
                SequenceNumber = 0x0;
            }
            var request = BuildHostPacket(command, data);
            for (var w = 0; w < MaxWriteRetries; w++) {
                // Write packet
                Channel.Write(request);
                // Read response
                var currentFrame = new List<byte>();
                for(;;) {
                    var buffer = Channel.Read();
                    var readFrames = new List<List<byte>>();
                    foreach(var b in buffer) {
                        currentFrame.Add(b);
                        // Split buffer by following separators
                        if (b == MarkerNak || b == MarkerSyn || b == MarkerTerminator) {
                            readFrames.Add(currentFrame);
                            currentFrame = new List<byte>();
                        }
                    }
                    var (wait, repeat) = (false, false);
                    foreach(var frame in readFrames) {
                        switch (frame[0]) {
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
                    if (wait) {
                        // The FiscalPrinter is still not ready, so make another read
                        continue;
                    }
                    if (repeat) {
                        // The FiscalPrinter cannot answer, so make the request again
                        break;
                    }
                }
            }
            return null;
        }

        protected string ParseResponse(byte[] rawResponse) {
            var (preamblePos, separatorPos, postamblePos, terminatorPos) = (0u, 0u, 0u, 0u);
            for(var i = 0u; i < rawResponse.Length; i++) {
                var b = rawResponse[i];
                switch (b) {
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
            if (preamblePos + 4 < separatorPos && separatorPos + 6 < postamblePos && postamblePos + 4 < terminatorPos) {
                var data = rawResponse.Slice(preamblePos+4, separatorPos);
                var status = rawResponse.Slice(separatorPos+1, postamblePos);
                var bcc = rawResponse.Slice(postamblePos+1, terminatorPos);
                var computedBcc = ComputeBCC(rawResponse.Slice(preamblePos+1, postamblePos+1));
                if (bcc.SequenceEqual(computedBcc)) {
                    // TODO: status parsing
                    return System.Text.Encoding.UTF8.GetString(data);
                }
            }
            return null;
        }

        protected string Request(byte command, string data) {
            return ParseResponse(RawRequest(command, System.Text.Encoding.ASCII.GetBytes(data)));
        }

        public string ReadRawDeviceInfo() {
            return Request(CommandGetDeviceInfo, "1");
        }
    }
} 
