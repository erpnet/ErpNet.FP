using ErpNet.Fiscal.Print.Core;
using ErpNet.Fiscal.Print.Drivers.BgDaisy;
using ErpNet.Fiscal.Print.Drivers.BgDatecs;
using ErpNet.Fiscal.Print.Drivers.BgEltrade;
using ErpNet.Fiscal.Print.Drivers.BgTremol;
using ErpNet.Fiscal.Print.Provider;
using ErpNet.Fiscal.Print.Transports;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace ErpNet.Fiscal.PrintExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Provider provider = new Provider();
            var comTransport = new ComTransport();
            var btTransport = new BtTransport();
            var httpTransport = new HttpTransport();

            // Cloud transport with account.
            var cloudPrintTransport = new CloudPrintTransport("user", "pwd");

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
            provider.Register(daisyIsl, comTransport);
            provider.Register(datecsPIsl, comTransport);
            provider.Register(datecsCIsl, comTransport);
            provider.Register(datecsXIsl, comTransport);
            provider.Register(eltradeIsl, comTransport);
            provider.Register(daisyIsl, btTransport);
            provider.Register(daisyJson, httpTransport);
            provider.Register(tremolZfp, httpTransport);
            provider.Register(erpNetJson, cloudPrintTransport);

            // Find all printers.
            var printers = provider.DetectAvailablePrinters();
            if (!printers.Any())
            {
                Console.WriteLine("No local printers found.");
                return;
            }
            Console.WriteLine($"Found {printers.Count()} printer(s):");
            foreach (KeyValuePair<string, IFiscalPrinter> printer in printers)
            {
                var info = printer.Value.DeviceInfo;
                Console.Write($"{info.Company} {info.Model}, ");
                Console.Write($"S/N: {info.SerialNumber}, FM S/N: {info.FiscalMemorySerialNumber}, ");
                Console.WriteLine($"URI: {printer.Key}");
            }

            // Now use Uri to connect to specific printer.
            //var uri = "bg.dt.x.isl.com://COM9";
            var uri = printers.First().Key;
            var fp = provider.Connect(uri, new Dictionary<string, string>
            {
                ["Operator.ID"] = "1",
                ["Operator.Password"] = "1"
            });

            // Connecting with different credentials
            var fpadm = provider.Connect(uri, new Dictionary<string, string>
            {
                ["Operator.ID"] = "20",
                ["Operator.Password"] = "9999"
            });

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
                        Discount = 10,
                        IsDiscountPercent = true,
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
            var result = fp.PrintReceipt(doc);
            Console.WriteLine(result.FiscalMemoryPosition);
            //fp.PrintZeroingReport();
        }
    }
}

