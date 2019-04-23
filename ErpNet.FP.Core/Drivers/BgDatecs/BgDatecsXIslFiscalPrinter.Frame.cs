using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDatecsXIslFiscalPrinter : BgIslFiscalPrinter
    {

        protected override byte[] BuildHostFrame(byte command, byte[]? data)
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

        protected override (string, DeviceStatus) ParseResponse(byte[]? rawResponse)
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
                    var response = Encoding.UTF8.GetString(data);
                    System.Diagnostics.Debug.WriteLine($"Response({data.Length}): {response}");

                    return (response, ParseStatus(status));
                }
            }
            throw new InvalidResponseException("the response is invalid");
        }

    }
}
