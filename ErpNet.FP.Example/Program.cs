using ErpNet.FP.Core;

namespace ErpNet.FP.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var devices = Provider.GetLocalDevices();
            if (devices.Length >= 1)
            {
                var fp = Provider.Connect(devices[0], new PrintOptions());


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
                                TaxGroup = 2
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
                              TaxGroup = 2
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


            }

        }
    }
}
