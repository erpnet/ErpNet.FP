namespace ErpNet.FP.Core.Drivers.BgIcp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Serilog;

    /// <summary>
    /// Fiscal printer using the Icp implementation of ISL Ltd.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIcpFiscalPrinter" />
    public partial class BgIcpFiscalPrinter : BgFiscalPrinter
    {
        public byte[]? DeviceNo = null;

        protected const byte
            MarkerWAIT = 0x05,
            MarkerNACK = 0x15,
            MarkerSTX = 0x02,
            MarkerETX = 0x03,
            MarkerACK = 0x06;
        protected const byte MaxWriteRetries = 6;
        protected const byte MaxReadRetries = 200;
        protected virtual byte[] BuildHostFrame(byte[]? data)
        {
            // Frame header
            var frame = new List<byte>
            {
                MarkerSTX
            };
            frame.AddRange(DeviceNo ?? PrinterEncoding.GetBytes("0000"));
            frame.AddRange(data);
            frame.AddRange(ByteTo2Bytes((byte)(10 + (data != null ? data.Length : 0))));
            frame.AddRange(ComputeCS(frame.ToArray()));
            frame.Add(MarkerETX);

            return frame.ToArray();
        }

        protected virtual byte[] ByteTo2Bytes(byte b)
        {
            return new byte[]{
                (byte)((b >> 4 & 0x0f) + 0x30),
                (byte)((b >> 0 & 0x0f) + 0x30)
            };
        }
        protected virtual byte[] ComputeCS(byte[] fragment)
        {
            byte sum = 0;
            foreach (byte b in fragment)
            {
                sum += b;
            }
            return ByteTo2Bytes(sum);
        }

        protected virtual byte[]? RawRequest(byte[]? data)
        {
            var deviceDescriptor = string.IsNullOrEmpty(DeviceInfo.Uri) ? Channel.Descriptor : DeviceInfo.Uri;

            var request = BuildHostFrame(data);
            for (var w = 0; w < MaxWriteRetries; w++)
            {

                // Write request frame
                Log.Information($"{deviceDescriptor} <<< {BitConverter.ToString(request)}");
                try
                {
                    Channel.Write(request);
                }
                catch (Exception ex)
                {
                    Log.Information($"{deviceDescriptor} Cannot write to channel: {ex.Message}");
                    continue;
                }

                // Read response frames.
                var currentFrame = new List<byte>();
                for (var r = 0; r < MaxReadRetries; r++)
                {
                    byte[] buffer;

                    try
                    {
                        buffer = Channel.Read();
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"{deviceDescriptor} Cannot read from channel: {ex.Message}");
                        return null;
                    }

                    // For debugging purposes only.  

                    Log.Information($"{deviceDescriptor} >>> {BitConverter.ToString(buffer)}");

                    // Parse frames
                    var readFrames = new List<List<byte>>();
                    foreach (var b in buffer)
                    {
                        currentFrame.Add(b);
                        // Split buffer by following separators
                        if (b == MarkerNACK || b == MarkerWAIT || b == MarkerETX || b == MarkerACK)
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
                            case MarkerNACK:
                                // Only last non-packed frame matters if there are many readed
                                // So change the state accordingly
                                (wait, repeat) = (false, true);
                                break;
                            case MarkerWAIT:
                                // Only last non-packed frame matters if there are many readed
                                // So change the state accordingly
                                (wait, repeat) = (true, false);
                                break;
                            case MarkerSTX:
                                // By the protocol, it is allowed only one packed frame response per request.
                                // So return first occurence of packed frame as response.
                                return frame.ToArray();
                            case MarkerACK:
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

        protected virtual string ParseResponse(byte[]? rawResponse)
        {
            var data = ParseRawResponse(rawResponse);

            if (data == null)
            {
                return string.Empty;
            }

            var response = PrinterEncoding.GetString(data);

            return response;
        }

        protected virtual byte[]? ParseRawResponse(byte[]? rawResponse)
        {
            if (rawResponse == null)
            {
                throw new InvalidResponseException("no response");
            }
            if (rawResponse.Length == 1 && rawResponse[0] == MarkerACK)
            {
                Log.Information($"Response(ACK)");
                return null;
            }
            var (stxPos, etxPos) = (0u, 0u);
            for (var i = 0u; i < rawResponse.Length; i++)
            {
                var b = rawResponse[i];
                switch (b)
                {
                    case MarkerSTX:
                        stxPos = i;
                        break;
                    case MarkerETX:
                        etxPos = i;
                        break;
                }
            }
            if (stxPos <= etxPos)
            {
                var data = rawResponse.Slice(stxPos + 1, etxPos - 4);
                var cs = rawResponse.Slice(etxPos - 2, etxPos);
                var len = rawResponse.Slice(etxPos - 4, etxPos - 2);
                var computedLen = ByteTo2Bytes((byte)(data.Length));
                var computedCS = ComputeCS(rawResponse.Slice(stxPos, etxPos - 4));
                if (cs.SequenceEqual(computedCS) && len.SequenceEqual(computedLen))
                {
                    return data;
                }
            }
            throw new InvalidResponseException("the response is invalid");
        }

        protected (string, DeviceStatus) Request(string? data = null)
        {
            lock (frameSyncLock)
            {
                try
                {
                    var response = ParseResponse(RawRequest(data == null ? null : PrinterEncoding.GetBytes(data)));
                    if (data == "00")
                    {
                        return (response, new DeviceStatus());                        
                    }
                    var rawStatus = ParseRawResponse(RawRequest(data == null ? null : PrinterEncoding.GetBytes("F80C")));
                    return (response, ParseStatus(rawStatus));
                }
                catch (InvalidResponseException e)
                {
                    var deviceStatus = new DeviceStatus();
                    deviceStatus.AddError("E107", e.Message);
                    return (string.Empty, deviceStatus);
                }
                catch (Exception e)
                {
                    var deviceStatus = new DeviceStatus();
                    deviceStatus.AddError("E101", e.Message);
                    return (string.Empty, deviceStatus);
                }
            }
        }

        protected override DeviceStatus ParseStatus(byte[]? rawStatus)
        {
            var deviceStatus = new DeviceStatus();
            if (rawStatus == null || rawStatus.Length != 12)
            {
                deviceStatus.AddError("E401", "Invalid status");
                return deviceStatus;
            }

            byte[] status = new byte[] { 0, 0, 0, 0, 0, 0 };
            for (var i = 0; i < status.Length; i++)
            {
                byte hi4 = (byte)((rawStatus[i * 2] - 0x30) << 4);
                byte lo4 = (byte)(rawStatus[i * 2 + 1] - 0x30);
                status[i] = (byte)(hi4 + lo4);
            }

            for (var i = 0; i < status.Length; i++)
            {
                byte mask = 0b10000000;
                byte b = status[i];
                for (var j = 0; j < 8; j++)
                {
                    if ((mask & b) != 0)
                    {
                        var (statusBitsCode, statusBitsText, statusBitStringType) = StatusBitsStrings[i * 8 + (7 - j)];
                        deviceStatus.AddMessage(new StatusMessage
                        {
                            Type = statusBitStringType,
                            Code = statusBitsCode,
                            Text = statusBitsText
                        });
                    }
                    mask >>= 1;
                }
            }
            return deviceStatus;
        }

    }
}
