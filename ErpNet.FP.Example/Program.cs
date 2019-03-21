using ErpNet.FP.Core;

namespace ErpNet.FP.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var devices = UriProvider.GetLocalDevices();
            if (devices.Length >= 1)
            {
                var fp = UriProvider.Connect(devices[0], new PrintOptions());

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
}
