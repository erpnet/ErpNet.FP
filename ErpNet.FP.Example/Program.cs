using System.Linq;
using ErpNet.FP.Print.Core;
using ErpNet.FP.Print.Drivers.BgDaisy;
using ErpNet.FP.Print.Drivers.BgTremol;
using ErpNet.FP.Print.Provider;
using ErpNet.FP.Print.Transports;

namespace ErpNet.FP.Example
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
            var daisyJson = new BgDaisyJsonFiscalPrinterDriver();
            var tremolZfp = new BgTremolZfpFiscalPrinterDriver();
            var erpNetJson = new ErpNetJsonDriver();

            // Add drivers and their compatible transports to the provider.
            provider.Add(daisyIsl, comTransport);
            provider.Add(daisyIsl, btTransport);
            provider.Add(daisyJson, httpTransport);
            provider.Add(tremolZfp, httpTransport);
            provider.Add(erpNetJson, cloudPrintTransport);

            // Find all printers.
            var printers = provider.DetectAvailablePrinters();
            if (!printers.Any())
            {
                System.Console.WriteLine("No local printers found.");
                return;
            }

            // Now use Uri to connect to specific printer.
            var fp = provider.Connect("bg.dy.json.http://printer.intranet.local");

            // Print a receipt.
            var doc = new Receipt()
            {
                UniqueSaleNumber = "00000000-0000-000",
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
                        Amount = 30,
                        PaymentType = PaymentType.Cash
                    }
                }
            };

            var result = fp.PrintReceipt(doc);
            System.Console.WriteLine(result.FiscalMemoryPosition);
        }
    }
}

