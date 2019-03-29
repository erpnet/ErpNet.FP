using ErpNet.Fiscal.Print.Core;
using ErpNet.Fiscal.Print.Drivers.BgDaisy;
using ErpNet.Fiscal.Print.Drivers.BgDatecs;
using ErpNet.Fiscal.Print.Drivers.BgEltrade;
using ErpNet.Fiscal.Print.Drivers.BgTremol;
using ErpNet.Fiscal.Print.Provider;
using ErpNet.Fiscal.Print.Transports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ErpNet.Fiscal.PrintExample
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestTremolPrinter();
            //TestSpecificPrinter();
            TestAutoDetect();
            //TestByUri();
        }

        static Provider GetProviderOfSupportedTransportsAndDrivers()
        {
            // Transports
            var comTransport = new ComTransport();
            var btTransport = new BtTransport();
            var httpTransport = new HttpTransport();

            // Cloud transport with account.
            var cloudPrintTransport = new CloudPrintTransport("user", "pwd");

            // Drivers
            var daisyIsl = new BgDaisyIslFiscalPrinterDriver();
            var datecsPIsl = new BgDatecsPIslFiscalPrinterDriver();
            var datecsCIsl = new BgDatecsCIslFiscalPrinterDriver();
            var datecsXIsl = new BgDatecsXIslFiscalPrinterDriver();
            var eltradeIsl = new BgEltradeIslFiscalPrinterDriver();
            var daisyJson = new BgDaisyJsonFiscalPrinterDriver();
            var tremolZfp = new BgTremolZfpFiscalPrinterDriver();

            // Add ErpNet Json driver, which can be used to forward the commands to:
            // 1. Another ErpNet print server.
            // 2. Cloud printing instance.
            var erpNetJson = new ErpNetJsonDriver();

            // Add drivers and their compatible transports to the provider.
            var provider = new Provider()
                //.Register(daisyIsl, comTransport)
                //.Register(datecsPIsl, comTransport)
                //.Register(datecsCIsl, comTransport)
                //.Register(datecsXIsl, comTransport)
                //.Register(eltradeIsl, comTransport)
                .Register(tremolZfp, comTransport)
                //.Register(daisyIsl, btTransport)
                //.Register(daisyJson, httpTransport)
                .Register(erpNetJson, cloudPrintTransport);

            return provider;
        }

        static void TestSpecificPrinter()
        {
            // One liner to connect to specific fiscal device, with specific options
            var datecsC = new Provider()
                .Register(new BgDatecsCIslFiscalPrinterDriver(), new ComTransport())
                .Connect("bg.dt.c.isl.com://COM13", new Dictionary<string, string>
                {
                    ["Operator.ID"] = "1",
                    ["Operator.Password"] = "1",
                    ["Administrator.ID"] = "20",
                    ["Administrator.Password"] = "9999"
                });
            ShowFiscalPrinterInfo(datecsC);
            TestAllMethods(datecsC);
        }

        static void TestTremolPrinter()
        {
            // One liner to connect to specific fiscal device, with specific options
            var tremol = new Provider()
                .Register(new BgTremolZfpFiscalPrinterDriver(), new ComTransport())
                .Connect("bg.tr.zfp.com://COM21", new Dictionary<string, string>
                {
                    ["Operator.ID"] = "1",
                    ["Operator.Password"] = "1",
                    ["Administrator.ID"] = "20",
                    ["Administrator.Password"] = "9999"
                });
            ShowFiscalPrinterInfo(tremol);
            //TestAllMethods(tremol);
        }

        static void TestAutoDetect()
        {
            // Find all printers.
            var printers = GetProviderOfSupportedTransportsAndDrivers().DetectAvailablePrinters();
            if (!printers.Any())
            {
                Console.WriteLine("No local printers found.");
                return;
            }
            Console.WriteLine($"Found {printers.Count()} printer(s):");
            foreach (KeyValuePair<string, IFiscalPrinter> printer in printers)
            {
                Console.Write($"URI: {printer.Key} - ");
                ShowFiscalPrinterInfo(printer.Value);
                TestAllMethods(printers.First().Value);
            }
        }

        static void TestByUri()
        {
            var provider = GetProviderOfSupportedTransportsAndDrivers();

            // Daisy CompactM, S/ N: DY448967, FM S/ N: 36607003
            TestAllMethods(provider.Connect("bg.dy.isl.com://COM5"));

            // Datecs FP-2000, S/N: DT279013, FM S/N: 02279013
            TestAllMethods(provider.Connect("bg.dt.p.isl.com://COM18"));

            // Datecs FP-700X, S/N: DT525860, FM S/N: 02525860
            TestAllMethods(provider.Connect("bg.dt.x.isl.com://COM7"));

            // Datecs DP-25, S/N: DT517985, FM S/N: 02525860
            TestAllMethods(provider.Connect("bg.dt.c.isl.com://COM13"));

            // Eltrade A1, S/N: ED311662, FM S/N: 44311662
            // With example with setting options while connecting
            TestAllMethods(provider.Connect("bg.ed.isl.com://COM19", new Dictionary<string, string>
            {
                ["Operator.Password"] = "1",
                ["Administrator.Password"] = "9999"
            }));
        }

        static void TestAllMethods(IFiscalPrinter fp)
        {
            // Print a receipt.
            var doc = new Receipt()
            {
                UniqueSaleNumber = "DT279013-DD01-0000001",
                Items = new Item[]
                {
                    new Item()
                    {
                        Text = "Сирене",
                        Quantity = 1,
                        UnitPrice = 12,
                        TaxGroup = TaxGroup.GroupB
                    },
                    new Item()
                    {
                        Text = "Допълнителен коментар към сиренето...",
                        IsComment  = true
                    },

                    new Item()
                    {
                        Text = "Кашкавал",
                        Quantity = 2,
                        UnitPrice = 10,
                        PriceModifierValue = 10,
                        PriceModifierType = PriceModifierType.DiscountPercent,
                        TaxGroup = TaxGroup.GroupB
                    }
                },
                Payments = new Payment[]
                {
                    new Payment()
                    {
                        Amount = 34,
                        PaymentType = PaymentType.Cash
                    }
                }
            };

            //fp.PrintMoneyDeposit(123.4m);
            //fp.PrintMoneyWithdraw(43.21m);
            //var result = fp.PrintReceipt(doc);
            //Console.WriteLine(result.FiscalMemoryPosition);
            //fp.PrintZeroingReport();
        }

        static void ShowFiscalPrinterInfo(IFiscalPrinter printer)
        {
            var info = printer.DeviceInfo;
            Console.WriteLine(
                $"{info.Company} {info.Model}, S/N: { info.SerialNumber}, FM S/N: { info.FiscalMemorySerialNumber}");
        }
    }
}


