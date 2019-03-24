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
                (byte)(((byte)(bccSum >> 0x0b) & 0x0f) + DigitZero), 
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
                var currentFragment = new List<byte>();
                for(;;) {
                    var buffer = Channel.Read();
                    var readFragments = new List<List<byte>>();
                    foreach(var b in buffer) {
                        Console.Write($"{b:X} ");
                        currentFragment.Add(b);
                        // Split buffer by following separators
                        if (b == MarkerNak || b == MarkerSyn || b == MarkerTerminator) {
                            readFragments.Add(currentFragment);
                            currentFragment = new List<byte>();
                        }
                    }
                    Console.WriteLine();
                    var (wait, repeat) = (false, false);
                    foreach(var fragment in readFragments) {
                        Console.Write("Fragment: ");
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(fragment.ToArray()));
                        switch (fragment[0]) {
                        case MarkerNak:
                            // Only last non-packed fragment matters if there are many readed
                            // So change the state accordingly
                            (wait, repeat) = (false, true);
                            break;
                        case MarkerSyn:
                            // Only last non-packed fragment matters if there are many readed
                            // So change the state accordingly
                            (wait, repeat) = (true, false);
                            break;
                        case MarkerPreamble:
                            // By the protocol, it is allowed only one packed response per request.
                            // So return first occurence of packed fragment as response.
                            return fragment.ToArray();
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

        protected byte[] ParseResponse(byte[] rawResponse) {
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
            Console.WriteLine($"{preamblePos} {separatorPos} {postamblePos} {terminatorPos}");
            if (preamblePos + 4 < separatorPos && separatorPos + 6 < postamblePos && postamblePos + 4 < terminatorPos) {
                var data = rawResponse.Slice(preamblePos+4, separatorPos);
                var status = rawResponse.Slice(separatorPos+1, postamblePos);
                var bcc = rawResponse.Slice(postamblePos+1, terminatorPos);
                if (bcc.SequenceEqual(ComputeBCC(rawResponse.Slice(preamblePos+1, postamblePos+1)))) {
                    // TODO: status parsing
                    Console.WriteLine("Response({0:X})=%s", status, data);
                    return data;
                }
            }
            return null;
        }

        protected byte[] Request(byte command, byte[] data) {
            return ParseResponse(RawRequest(command, data));
        }

        public string ReadRawDeviceInfo() {
            return System.Text.Encoding.UTF8.GetString(Request(CommandGetDeviceInfo, new byte[]{DigitOne}));
        }
    }
} 
