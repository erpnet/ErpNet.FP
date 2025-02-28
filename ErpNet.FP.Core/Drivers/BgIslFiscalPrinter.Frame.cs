﻿namespace ErpNet.FP.Core.Drivers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Serilog;

    public abstract partial class BgIslFiscalPrinter : BgFiscalPrinter
    {
        private static readonly Random random = new Random();
        protected const byte
            MarkerSpace = 0x20,
            MarkerSyn = 0x16,
            MarkerNak = 0x15,
            MarkerPreamble = 0x01,
            MarkerPostamble = 0x05,
            MarkerSeparator = 0x04,
            MarkerTerminator = 0x03;
        protected const byte MaxSequenceNumber = 0x7F - MarkerSpace;
        protected const byte MaxWriteRetries = 6;
        protected const byte MaxReadRetries = 200;
        protected byte FrameSequenceNumber = (byte)random.Next(0, MaxSequenceNumber - 1);

        protected virtual byte[] UInt16To4Bytes(UInt16 word)
        {
            return new byte[]{
                (byte)((word >> 12 & 0x0f) + 0x30),
                (byte)((word >> 8 & 0x0f) + 0x30),
                (byte)((word >> 4 & 0x0f) + 0x30),
                (byte)((word >> 0 & 0x0f) + 0x30)
            };
        }
        protected virtual byte[] ComputeBCC(byte[] fragment)
        {
            UInt16 bccSum = 0;
            foreach (byte b in fragment)
            {
                bccSum += b;
            }
            return UInt16To4Bytes(bccSum);
        }

        protected virtual byte[] BuildHostFrame(byte command, byte[]? data)
        {
            // Frame header
            var frame = new List<byte>
            {
                MarkerPreamble,
                (byte)(MarkerSpace + 4 + (data != null ? data.Length : 0)),
                (byte)(MarkerSpace + FrameSequenceNumber),
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

        protected virtual byte[]? RawRequest(byte command, byte[]? data)
        {
            var deviceDescriptor = string.IsNullOrEmpty(DeviceInfo.Uri) ? Channel.Descriptor : DeviceInfo.Uri;
            var beginTime = DateTime.Now;

            FrameSequenceNumber++;
            if (FrameSequenceNumber > MaxSequenceNumber)
            {
                FrameSequenceNumber = 0;
            }
            var request = BuildHostFrame(command, data);
            for (var w = 0; w < MaxWriteRetries; w++)
            {
                // Write request frame
                Log.Information($"{deviceDescriptor} <<< {BitConverter.ToString(request)}");
                try
                {
                    Channel.Write(request);
                }
                catch (TimeoutException)
                {
                    throw;      // nothing to do
                }
                catch (Exception ex)
                {
                    Log.Information($"{deviceDescriptor} Cannot write to channel: {ex.Message}");
                    throw;  // nothing to do
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
                        // If sended command is CommandFiscalReceiptTotal and payment is "Pay by Card using pinpad" ("2")
                        if (Info.UsePaymentTerminal && this is BgDatecs.BgDatecsXIslFiscalPrinter && 
                            command == CommandFiscalReceiptTotal && data != null && data.Length > 0 && data[0] == '2')
                            r -= 1;     // set no limit of reads when receiving MarkerSyn for this payment type only
                        continue;
                    }
                    if (repeat)
                    {
                        // The FiscalPrinter cannot answer, so make the request again
                        break;
                    }
                    if (buffer.Count() > 0 && buffer[0] == 0)
                    {
                        throw new InvalidResponseException("The response is invalid. Probably the communication speed not match or the device is not a fiscal device!");
                    }
                }
            }
            return null;
        }

        protected virtual (string, DeviceStatus) ParseResponse(byte[]? rawResponse)
        {
            if (rawResponse == null)
            {
                throw new InvalidResponseException("no response");
            }
            var (preamblePos, separatorPos, postamblePos, terminatorPos) = (0u, 0u, 0u, 0u);
            for (uint i = 0u; i < rawResponse.Length; i++)
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
                    var response = PrinterEncoding.GetString(data);

                    return (response, ParseStatus(status));
                }
            }
            throw new InvalidResponseException("The response is invalid. Checksum does not match.");
        }

        public override DeviceStatusWithRawResponse RawRequest(RequestFrame requestFrame)
        {
            if (requestFrame.RawRequest.Length == 0)
            {
                var deviceStatus = new DeviceStatus();
                deviceStatus.AddError("E401", "Request length must be at least 1 character");
                return new DeviceStatusWithRawResponse(deviceStatus);
            }
            var cmd = PrinterEncoding.GetBytes(requestFrame.RawRequest.Substring(0, 1))[0];
            var data = requestFrame.RawRequest.Substring(1);
            var (rawResponse, status) = Request(cmd, data);
            return new DeviceStatusWithRawResponse(status) { RawResponse = rawResponse };
        }

        protected (string, DeviceStatus) Request(byte command, string? data = null)
        {            
            lock (frameSyncLock)
            {
                if (DeadLine < DateTime.Now)
                {
                    var deviceStatus = new DeviceStatus();
                    deviceStatus.AddError("E999", "User timeout occured while sending the request");
                    return (string.Empty, deviceStatus);
                }
                try
                {
                    return ParseResponse(RawRequest(command, data == null ? null : PrinterEncoding.GetBytes(data)));
                }
                catch (InvalidResponseException e)
                {
                    var deviceStatus = new DeviceStatus();
                    deviceStatus.AddError("E107", e.Message);
                    return (string.Empty, deviceStatus);
                }
                catch (FileNotFoundException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    var deviceStatus = new DeviceStatus();
                    deviceStatus.AddError("E101", e.Message);
                    return (string.Empty, deviceStatus);
                }
            }
        }
    }
}