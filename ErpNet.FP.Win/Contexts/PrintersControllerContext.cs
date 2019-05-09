using ErpNet.FP.Core;
using ErpNet.FP.Core.Drivers.BgDaisy;
using ErpNet.FP.Core.Drivers.BgDatecs;
using ErpNet.FP.Core.Drivers.BgEltrade;
using ErpNet.FP.Core.Drivers.BgTremol;
using ErpNet.FP.Core.Provider;
using ErpNet.FP.Win.Transports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

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
