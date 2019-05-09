using ErpNet.FP.Core;
using ErpNet.FP.Core.Drivers.BgDaisy;
using ErpNet.FP.Core.Drivers.BgDatecs;
using ErpNet.FP.Core.Drivers.BgEltrade;
using ErpNet.FP.Core.Drivers.BgTremol;
using ErpNet.FP.Core.Provider;
using ErpNet.FP.Win.Transports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace ErpNet.FP.Win.Contexts
{
    public interface IPrintersControllerContext
    {
        Dictionary<string, DeviceInfo> PrintersInfo { get; }

        Dictionary<string, IFiscalPrinter> Printers { get; }
    }

    public class PrintersControllerContext : IPrintersControllerContext
    {
        private readonly ILogger logger;

        public class PrinterConfig
        {
            public string Uri { get; set; } = string.Empty;
        }

        public Provider Provider { get; } = new Provider();
        public Dictionary<string, DeviceInfo> PrintersInfo { get; } = new Dictionary<string, DeviceInfo>();

        public Dictionary<string, IFiscalPrinter> Printers { get; } = new Dictionary<string, IFiscalPrinter>();

        public static DateTime GetNetworkTime()
        {
            const string ntpServer = "pool.ntp.org";
            var ntpData = new byte[48];
            ntpData[0] = 0x1B; // LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 3000; // 3 seconds, prevents hanging when there is no connection
            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
            ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }

        public void AddPrinter(IFiscalPrinter printer)
        {
            // We use serial number of local connected fiscal printers as Printer ID
            var baseID = printer.DeviceInfo.SerialNumber.ToLowerInvariant();

            var printerID = baseID;
            int duplicateNumber = 0;
            while (PrintersInfo.ContainsKey(printerID))
            {
                duplicateNumber++;
                printerID = $"{baseID}_{duplicateNumber}";
            }
            PrintersInfo.Add(printerID, printer.DeviceInfo);
            Printers.Add(printerID, printer);
            logger.LogInformation($"Found {printerID}: {printer.DeviceInfo.Uri}");
        }

        public PrintersControllerContext(IConfiguration configuration, ILogger logger)
        {
            this.logger = logger;

            try
            {
                logger.LogInformation("NTP Time: " + GetNetworkTime().ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
            }
            catch
            {
                logger.LogCritical("NTP Time cannot be read!");
            }

            var autoDetect = configuration.GetValue<bool>("AutoDetect", true);

            // Transports
            var comTransport = new ComTransport();

            // Drivers
            var daisyIsl = new BgDaisyIslFiscalPrinterDriver();
            var datecsPIsl = new BgDatecsPIslFiscalPrinterDriver();
            var datecsCIsl = new BgDatecsCIslFiscalPrinterDriver();
            var datecsXIsl = new BgDatecsXIslFiscalPrinterDriver();
            var eltradeIsl = new BgEltradeIslFiscalPrinterDriver();
            var tremolZfp = new BgTremolZfpFiscalPrinterDriver();
            var tremolV2Zfp = new BgTremolZfpV2FiscalPrinterDriver();

            // Add drivers and their compatible transports to the provider.
            var provider = new Provider()
                .Register(daisyIsl, comTransport)
                .Register(datecsCIsl, comTransport)
                .Register(datecsPIsl, comTransport)
                .Register(eltradeIsl, comTransport)
                .Register(datecsXIsl, comTransport)
                .Register(tremolZfp, comTransport)
                .Register(tremolV2Zfp, comTransport);

            if (autoDetect)
            {
                logger.LogInformation("Autodetecting local printers...");
                var printers = provider.DetectAvailablePrinters();
                foreach (KeyValuePair<string, IFiscalPrinter> printer in printers)
                {
                    AddPrinter(printer.Value);
                }
            }

            logger.LogInformation("Detecting configured printers...");
            var printersSettings = configuration.GetSection("Printers").Get<Dictionary<string, PrinterConfig>>();
            foreach (var printerSetting in printersSettings)
            {
                string logString = $"Trying {printerSetting.Key}: {printerSetting.Value.Uri}";
                var uri = printerSetting.Value.Uri;
                if (uri.Length > 0)
                {
                    try
                    {
                        var printer = provider.Connect(printerSetting.Value.Uri, null);
                        logger.LogInformation($"{logString}, OK");
                        PrintersInfo.Add(printerSetting.Key, printer.DeviceInfo);
                        Printers.Add(printerSetting.Key, printer);
                    }
                    catch
                    {
                        logger.LogInformation($"{logString}, failed");
                        // Do not add this printer, it fails to connect.
                    }
                }
            }

            logger.LogInformation($"Detecting done. Found {Printers.Count} available printer(s).");
        }
    }
}
