using System.Linq;
using ErpNet.FP.Print.Core;
using ErpNet.FP.Print.Provider;

namespace ErpNet.FP.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var devices = Provider.DetectLocalDevices();
            var d = devices.FirstOrDefault();
            if (d == null)
            {
                System.Console.WriteLine("No local printers found.");
                return;
            }

            var fp = Provider.Connect(d.Address);

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

