using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ErpNet.FP.Core;

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
        protected const byte MaxWriteRetries = 6;
        protected const byte MaxReadRetries = 200;
        protected const uint MaxPingRetries = 1000;

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

        protected virtual byte[] BuildHostFrame(byte command, byte[] data)
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
            for (var r = 0; r < MaxPingRetries; r++)
            {
                Channel.Write(new byte[] { SpecialCommandPing });
                var buffer = Channel.Read();
                if (buffer[0] == PingAnswerDeviceReady)
                {
                    return;
                }
            }
            throw new PingRetriesCountExhausted();
        }

        protected virtual byte[] RawRequest(byte command, byte[] data)
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
                System.Diagnostics.Debug.Write(">>>");
                foreach (var b in request)
                {
                    System.Diagnostics.Debug.Write($"{b:X} ");
                }
                System.Diagnostics.Debug.WriteLine("");
                Channel.Write(request);

                // Read response frames.
                var currentFrame = new List<byte>();
                for (var r = 0; r < MaxReadRetries; r++)
                {
                    var buffer = Channel.Read();

                    // For debugging purposes only.
                    System.Diagnostics.Debug.Write("<<<");
                    foreach (var b in buffer)
                    {
                        System.Diagnostics.Debug.Write($"{b:X} ");
                    }
                    System.Diagnostics.Debug.WriteLine("");

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

        protected virtual (string, DeviceStatus) ParseResponse(byte[] rawResponse)
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
                    return ("", ParseStatus(data));
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
                    var response = Encoding.UTF8.GetString(data);
                    System.Diagnostics.Debug.WriteLine($"Response({data.Length}): {response}");
                    return (response, ParseStatus(null));
                }
            } 

            throw new InvalidResponseException("the response is invalid");
        }

        protected override DeviceStatus ParseStatus(byte[] status)
        {
            // For debugging purposes only (to view status bits)    
            var deviceID = (Info == null ? "" : Info.SerialNumber);
            System.Diagnostics.Debug.WriteLine($"Status of device {deviceID}");
            if (status == null)
            {
                System.Diagnostics.Debug.WriteLine("No status");
            }
            else
            {
                foreach(var b in status)
                {
                    System.Diagnostics.Debug.Write($"{b:X} ");
                }
                System.Diagnostics.Debug.WriteLine("");
            }
            

            // TODO: fill the device status
            return new DeviceStatus();
        }

        protected (string, DeviceStatus) Request(byte command, string data)
        {
            System.Diagnostics.Debug.WriteLine($"Request({command:X}): '{data}'");
            return ParseResponse(RawRequest(command, PrinterEncoding.GetBytes(data)));
        }

        protected (string, DeviceStatus) Request(byte command)
        {
            System.Diagnostics.Debug.WriteLine($"Request({command:X})");
            return ParseResponse(RawRequest(command, null));
        }
    }
}
