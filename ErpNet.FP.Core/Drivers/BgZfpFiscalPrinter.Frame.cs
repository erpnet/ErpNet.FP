using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ErpNet.FP.Core.Drivers
{
    public partial class BgZfpFiscalPrinter : BgFiscalPrinter
    {
        protected byte FrameSequenceNumber = 0;
        protected const byte
            MarkerSpace = 0x20,
            MarkerNACK = 0x15,
            MarkerRETRY = 0x0e,
            MarkerSTX = 0x02,
            MarkerACK = 0x06,
            MarkerETX = 0x0A;
        protected const byte
            PingAnswerDeviceReady = 0x40,
            SpecialCommandPing = 0x09;
        protected const byte MaxSequenceNumber = 0x9F - MarkerSpace;
        protected const byte MaxWriteRetries = 3;
        protected const byte MaxReadRetries = 200;

        protected virtual byte[] ByteTo2Bytes(UInt16 word)
        {
            return new byte[]{
                (byte)((word >> 4 & 0x0f) + 0x30),
                (byte)((word >> 0 & 0x0f) + 0x30)
            };
        }
        protected virtual byte[] ComputeCS(byte[] fragment)
        {
            byte bccSum = 0;
            foreach (byte b in fragment)
            {
                bccSum ^= b;
            }
            return ByteTo2Bytes(bccSum);
        }

        protected virtual byte[] BuildHostFrame(byte command, byte[]? data)
        {
            // Frame header
            var frame = new List<byte>
            {
                MarkerSTX,
                (byte)(MarkerSpace + 3 + (data != null ? data.Length : 0)),
                (byte)(MarkerSpace + FrameSequenceNumber),
                command
            };

            // Frame data
            if (data != null)
            {
                frame.AddRange(data);
            }

            // Frame footer
            frame.AddRange(ComputeCS(frame.Skip(1).ToArray()));
            frame.Add(MarkerETX);

            return frame.ToArray();
        }

        protected void WaitForDeviceToBeReady()
        {
            System.Diagnostics.Trace.Write(">>> Ping ");
            for (; ; )
            {
                byte[]? buffer = null;
                for (var w = 0; w < MaxWriteRetries; w++)
                {
                    System.Diagnostics.Trace.Write($">>> {SpecialCommandPing:X} <<< ");
                    Channel.Write(new byte[] { SpecialCommandPing });
                    try
                    {
                        buffer = Channel.Read();
                        break;
                    }
                    catch (TimeoutException)
                    {
                        // When the device is too busy and cannot even answer ping
                        // It could be read timeout, so we will ping again, until
                        // MaxWriteRetries is exausted.
                        System.Diagnostics.Trace.WriteLine("Timeout, try again!");
                        System.Diagnostics.Trace.Write(">>> Ping ");
                        continue;
                    }
                }
                if (buffer == null || buffer.Length == 0)
                {
                    throw new TimeoutException("ping timeout");
                }
                var b = buffer[0];
                System.Diagnostics.Trace.Write($"{b:X} ");
                if (b == PingAnswerDeviceReady)
                {
                    System.Diagnostics.Trace.WriteLine("Ready!");
                    return;
                }
            }
            throw new TimeoutException("ping timeout");
        }

        protected virtual byte[]? RawRequest(byte command, byte[]? data)
        {
            FrameSequenceNumber++;
            if (FrameSequenceNumber > MaxSequenceNumber)
            {
                FrameSequenceNumber = 0;
            }
            // Wait for device to be ready
            // It will generate exception if there is read timeout
            WaitForDeviceToBeReady();
            var request = BuildHostFrame(command, data);
            for (var w = 0; w < MaxWriteRetries; w++)
            {
                // Write request frame
                System.Diagnostics.Trace.Write(">>>");
                foreach (var b in request)
                {
                    System.Diagnostics.Trace.Write($"{b:X} ");
                }
                System.Diagnostics.Trace.WriteLine("");
                Channel.Write(request);

                // Read response frames.
                var currentFrame = new List<byte>();
                for (var r = 0; r < MaxReadRetries; r++)
                {
                    var buffer = Channel.Read();

                    // For debugging purposes only.
                    System.Diagnostics.Trace.Write("<<<");
                    foreach (var b in buffer)
                    {
                        System.Diagnostics.Trace.Write($"{b:X} ");
                    }
                    System.Diagnostics.Trace.WriteLine("");

                    var readFrames = new List<List<byte>>();
                    foreach (var b in buffer)
                    {
                        currentFrame.Add(b);
                        // Split buffer by following separators
                        if (b == MarkerNACK || b == MarkerRETRY || b == MarkerETX)
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
                            case MarkerRETRY:
                                // Only last non-packed frame matters if there are many readed
                                // So change the state accordingly
                                (wait, repeat) = (true, false);
                                break;
                            case MarkerACK:
                                // By the protocol, it is allowed only one ack frame response per request.
                                // So if there is no error, we will wait for data frame
                                return frame.ToArray();
                            case MarkerSTX:
                                // By the protocol, it is allowed only one data frame response per request.
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

        protected virtual (string, DeviceStatus) ParseResponse(byte[]? rawResponse)
        {
            var (response, status) = ParseResponseAsByteArray(rawResponse);
            if (response == null)
            {
                return ("", status);
            }
            return (Encoding.UTF8.GetString(response), status);
        }

        protected virtual (byte[]?, DeviceStatus) ParseResponseAsByteArray(byte[]? rawResponse)
        {
            if (rawResponse == null)
            {
                throw new InvalidResponseException("no response");
            }
            var ackMode = true;
            var (dataStartPos, dataEndPos) = (0u, 0u);
            for (var i = 0u; i < rawResponse.Length; i++)
            {
                var b = rawResponse[i];
                switch (b)
                {
                    case MarkerACK:
                        dataStartPos = i;
                        ackMode = true;
                        break;
                    case MarkerSTX:
                        dataStartPos = i;
                        ackMode = false;
                        break;
                    case MarkerETX:
                        dataEndPos = i;
                        break;
                }
            }

            if (ackMode)
            {
                // Parse ack frame
                var checkSumPos = dataEndPos - 2u;
                var (msgStartPos, msgEndPos) = (dataStartPos + 2u, checkSumPos);
                var data = rawResponse.Slice(msgStartPos, msgEndPos);
                var cs = rawResponse.Slice(checkSumPos, dataEndPos);
                var computedCS = ComputeCS(rawResponse.Slice(dataStartPos + 1u, msgEndPos));
                if (cs.SequenceEqual(computedCS))
                {
                    return (null, ParseStatus(data));
                }
            }
            else
            {
                // Parse data frame
                var checkSumPos = dataEndPos - 2u;
                var (msgStartPos, msgEndPos) = (dataStartPos + 4u, checkSumPos);
                var data = rawResponse.Slice(msgStartPos, msgEndPos);
                var cs = rawResponse.Slice(checkSumPos, dataEndPos);
                var computedCS = ComputeCS(rawResponse.Slice(dataStartPos + 1u, msgEndPos));
                if (cs.SequenceEqual(computedCS))
                {
                    return (data, ParseStatus(null));
                }
            }

            throw new InvalidResponseException("The response is invalid. Checksum does not match.");
        }

        protected (string, DeviceStatus) Request(byte command, string? data = null)
        {
            lock (frameSyncLock)
            {
                try
                {
                    System.Diagnostics.Trace.WriteLine($"Request({command:X}): '{data}'");
                    return ParseResponse(RawRequest(command, data == null ? null : PrinterEncoding.GetBytes(data)));
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
    }
}
