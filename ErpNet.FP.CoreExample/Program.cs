using ErpNet.FP.Core;
using ErpNet.FP.Core.Drivers.BgDaisy;
using ErpNet.FP.Core.Drivers.BgDatecs;
using ErpNet.FP.Core.Drivers.BgEltrade;
using ErpNet.FP.Core.Drivers.BgTremol;
using ErpNet.FP.Core.Provider;
using ErpNet.FP.Core.Transports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ErpNet.FP.CoreExample
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestTremolPrinter();
            //TestEltradePrinter();
            //TestDatecsXComPrinter();
            //TestDatecsXTcpPrinter();
            TestDaisyPrinter();
            //TestSpecificPrinter();
            //TestAutoDetect();
            //TestByUri();
        }

        static Provider GetProviderOfSupportedTransportsAndDrivers()
        {
            // Transports
            var comTransport = new ComTransport();
            //var btTransport = new BtTransport();
            //var httpTransport = new HttpTransport();

            // Cloud transport with account.
            // var cloudPrintTransport = new CloudPrintTransport("user", "pwd");

            // Drivers
            var daisyIsl = new BgDaisyIslFiscalPrinterDriver();
            var datecsPIsl = new BgDatecsPIslFiscalPrinterDriver();
            var datecsCIsl = new BgDatecsCIslFiscalPrinterDriver();
            var datecsXIsl = new BgDatecsXIslFiscalPrinterDriver();
            var eltradeIsl = new BgEltradeIslFiscalPrinterDriver();
            var tremolZfp = new BgTremolZfpFiscalPrinterDriver();
            var tremolV2Zfp = new BgTremolZfpV2FiscalPrinterDriver();

            // Add ErpNet Json driver, which can be used to forward the commands to:
            // 1. Another ErpNet print server.
            // 2. Cloud printing instance.
            // var erpNetJson = new ErpNetJsonDriver();

            // Add drivers and their compatible transports to the provider.
            var provider = new Provider()
                .Register(daisyIsl, comTransport)
                .Register(datecsCIsl, comTransport)
                .Register(datecsPIsl, comTransport)
                .Register(eltradeIsl, comTransport)
                .Register(datecsXIsl, comTransport)
                .Register(tremolZfp, comTransport)
                .Register(tremolV2Zfp, comTransport);

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

        static void TestDaisyPrinter()
        {
            var daisy = new Provider()
                .Register(new BgDaisyIslFiscalPrinterDriver(), new ComTransport())
                .Connect("bg.dy.isl.com://COM3");
            ShowFiscalPrinterInfo(daisy);
            TestAllMethods(daisy);
        }

        static void TestEltradePrinter()
        {
            var eltrade = new Provider()
                .Register(new BgEltradeIslFiscalPrinterDriver(), new ComTransport())
                .Connect("bg.ed.isl.com://COM14");
            ShowFiscalPrinterInfo(eltrade);
            TestAllMethods(eltrade);
        }

        static void TestDatecsXComPrinter()
        {
            var datecsX = new Provider()
                .Register(new BgDatecsXIslFiscalPrinterDriver(), new ComTransport())
                .Connect("bg.dt.x.isl.com://COM21");
            ShowFiscalPrinterInfo(datecsX);
            TestAllMethods(datecsX);
        }

        static void TestDatecsXTcpPrinter()
        {
            var datecsX = new Provider()
                .Register(new BgDatecsXIslFiscalPrinterDriver(), new TcpTransport())
                .Connect("bg.dt.x.isl.tcp://10.10.1.208:4999");
            ShowFiscalPrinterInfo(datecsX);
            TestAllMethods(datecsX);
        }

        static void TestTremolPrinter()
        {
            // One liner to connect to specific fiscal device, with specific options
            var tremol = new Provider()
                .Register(new BgTremolZfpFiscalPrinterDriver(), new ComTransport())
                .Connect("bg.zk.zfp.com://COM3");
            ShowFiscalPrinterInfo(tremol);
            TestAllMethods(tremol);
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
                /*
                try
                {
                    TestAllMethods(printers.First().Value);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
                */
            }
        }

        static void TestByUri()
        {
            /*
            URI: bg.dy.isl.com://COM12 - Daisy CompactM, S/N: DY448967, FM S/N: 36607003
            URI: bg.dt.c.isl.com://COM7 - Datecs DP-25, S/N: DT517985, FM S/N: 02517985
            URI: bg.dt.p.isl.com://COM6 - Datecs FP-2000, S/N: DT279013, FM S/N: 02279013
            URI: bg.dy.isl.com://COM5 - Daisy CompactM, S/N: DY448967, FM S/N: 36607003
            URI: bg.dt.x.isl.com://COM8 - Datecs FP-700X, S/N: DT525860, FM S/N: 02525860
            URI: bg.ed.isl.com://COM4 - Eltrade A1, S/N: ED311662, FM S/N: 44311662
            URI: bg.zk.zfp.com://COM3 - Tremol M20, S/N: ZK126720, FM S/N: 50163145
             */

            var provider = GetProviderOfSupportedTransportsAndDrivers();

            // Daisy CompactM, S/N: DY448967, FM S/N: 36607003
            //TestAllMethods(provider.Connect("bg.dy.isl.com://COM12"));

            // Datecs FP-2000, S/N: DT279013, FM S/N: 02279013
            //TestAllMethods(provider.Connect("bg.dt.p.isl.com://COM6"));

            // Datecs Datecs DP-25, S/N: DT517985, FM S/N: 02517985
            //TestAllMethods(provider.Connect("bg.dt.c.isl.com://COM7"));

            // Datecs FP-700X, S/N: DT525860, FM S/N: 02525860
            TestAllMethods(provider.Connect("bg.dt.x.isl.com://COM8"));

            // Tremol M20, S/N: ZK126720, FM S/N: 50163145
            //TestAllMethods(provider.Connect("bg.zk.zfp.com://COM3"));

            // Eltrade A1, S/N: ED311662, FM S/N: 44311662
            // With example with setting options while connecting
            //TestAllMethods(provider.Connect("bg.ed.isl.com://COM4"));
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
                        TaxGroup = TaxGroup.TaxGroup2
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
                        TaxGroup = TaxGroup.TaxGroup2,
                        PriceModifierValue = 10,
                        PriceModifierType = PriceModifierType.DiscountPercent
                    }
                },
                Payments = new Payment[]
                {
                    new Payment()
                    {
                        Amount = 30,
                        PaymentType = PaymentType.Cash
                    }
                }
            };

            //fp.PrintMoneyDeposit(123.4m);
            //fp.PrintMoneyWithdraw(43.21m);
            var (result, commandStatus) = fp.PrintReceipt(doc);
            ShowStatus(commandStatus);

            Console.WriteLine($"Receipt Number: {result.ReceiptNumber}, DateTime: {result.ReceiptDateTime}, Amount: {result.ReceiptAmount}");

            /*
            var 
            
            Doc = new ReversalReceipt
            {
                Reason = ReversalReason.OperatorError,
                ReceiptNumber = result.ReceiptNumber,
                ReceiptDateTime = result.ReceiptDateTime,
                FiscalMemorySerialNumber = result.FiscalMemorySerialNumber
            }.CloneReceipt(doc);
            commandStatus = fp.PrintReversalReceipt(reverseDoc);
            ShowStatus(commandStatus);
            */

            //fp.PrintZReport();
        }

        static void ShowStatus(DeviceStatus status)
        {
            if (status.Ok)
            {
                Console.WriteLine("Status: Ok!");
            }
            else
            {
                Console.WriteLine("Errors: {0}", string.Join(", ", status.Errors));
                Console.WriteLine("Warnings: {0}", string.Join(", ", status.Warnings));
                Console.WriteLine("Statuses: {0}", string.Join(", ", status.Statuses));
            }
        }

        static void ShowFiscalPrinterInfo(IFiscalPrinter printer)
        {
            var info = printer.DeviceInfo;
            Console.WriteLine(
                $"{info.Company} {info.Model}, S/N: { info.SerialNumber}, FM S/N: { info.FiscalMemorySerialNumber}");
        }
    }
}


