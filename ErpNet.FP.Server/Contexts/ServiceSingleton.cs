using ErpNet.FP.Core.Configuration;
using ErpNet.FP.Core.Drivers.BgDaisy;
using ErpNet.FP.Core.Drivers.BgDatecs;
using ErpNet.FP.Core.Drivers.BgEltrade;
using ErpNet.FP.Core.Drivers.BgIcp;
using ErpNet.FP.Core.Drivers.BgIncotex;
using ErpNet.FP.Core.Drivers.BgTremol;
using ErpNet.FP.Core.Provider;
using ErpNet.FP.Core.Service;
using ErpNet.FP.Core.Transports;
using ErpNet.FP.Server.Configuration;
using System.Collections.Generic;

namespace ErpNet.FP.Server.Contexts
{
    /// <summary>
    /// ServiceSingleton is wrapper around ServiceControllerContext, that
    /// introduces writableConfigOptions through dependency injection, 
    /// and setting up the Provider
    /// </summary>
    public class ServiceSingleton : ServiceControllerContext
    {

        private readonly IWritableOptions<ServiceOptions> writableConfigOptions;

        public ServiceSingleton(
            IWritableOptions<ServiceOptions> writableConfigOptions)
            : base()
        {
            this.writableConfigOptions = writableConfigOptions;
            configOptions = writableConfigOptions.Value;
            Setup();
        }

        protected override void SetupProvider()
        {
            // Transports
            var comTransport = new ComTransport();
            var tcpTransport = new TcpTransport();

            // Drivers
            var datecsXIsl = new BgDatecsXIslFiscalPrinterDriver();
            var datecsPIsl = new BgDatecsPIslFiscalPrinterDriver();
            var datecsCIsl = new BgDatecsCIslFiscalPrinterDriver();
            var eltradeIsl = new BgEltradeIslFiscalPrinterDriver();
            var daisyIsl = new BgDaisyIslFiscalPrinterDriver();
            var incotexIsl = new BgIncotexIslFiscalPrinterDriver();
            var islIcp = new BgIslIcpFiscalPrinterDriver();
            var tremolZfp = new BgTremolZfpFiscalPrinterDriver();
            var tremolV2Zfp = new BgTremolZfpV2FiscalPrinterDriver();

            // Add drivers and their compatible transports to the provider.
            Provider = new Provider()
                // Isl X Frame
                .Register(datecsXIsl, comTransport)
                .Register(datecsXIsl, tcpTransport)
                // Isl Frame
                .Register(datecsCIsl, comTransport)
                .Register(datecsCIsl, tcpTransport)
                .Register(datecsPIsl, comTransport)
                .Register(datecsPIsl, tcpTransport)
                .Register(eltradeIsl, comTransport)
                .Register(eltradeIsl, tcpTransport)
                // Isl Frame + constants
                .Register(daisyIsl, comTransport)
                .Register(daisyIsl, tcpTransport)
                .Register(incotexIsl, comTransport)
                .Register(incotexIsl, tcpTransport)
                // Icp Frame
                .Register(islIcp, comTransport)
                .Register(islIcp, tcpTransport)
                // Zfp Frame
                .Register(tremolZfp, comTransport)
                .Register(tremolZfp, tcpTransport)
                .Register(tremolV2Zfp, comTransport)
                .Register(tremolV2Zfp, tcpTransport);
        }

        protected override void WriteOptions()
        {
            writableConfigOptions.Update(updatedConfigOptions =>
            {
                updatedConfigOptions.AutoDetect = configOptions.AutoDetect;
                updatedConfigOptions.ServerId = configOptions.ServerId;
                updatedConfigOptions.Printers = configOptions.Printers ?? new Dictionary<string, PrinterConfig>();
            });
        }
    }

}