using System;
using ErpNet.FP.Core;

namespace ErpNet.FP
{
    public class FiscalPrinter : IFiscalPrinter
    {
        internal IFiscalPrinter GetPrinter(Type type)
        {
            object instance;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                throw new FiscalPrinterNotConstructedException($"Failed to construct fiscal printer of type {type}", e);
                
            }

            var result = instance as IFiscalPrinter;
            if (result == null)
            {
                throw new TypeIsNotFiscalPrinterException($"Type {type} is not ${nameof(IFiscalPrinter)}");
            }
            return result;
        }

        public Type[] GetDrivers()
        {
            return new Type[]
            {
                typeof(Tremol.Zfp.TremolZfpFiscalPrinter)
            };
        }

        public void DepositMoney(FiscalPrinterState state, decimal sum)
        {
            GetPrinter(state.Driver).DepositMoney(state, sum);
        }

        public void Dispose()
        {
        }

        public FiscalDeviceInfo GetDeviceInfo(FiscalPrinterState state)
        {
            return GetPrinter(state.Driver).GetDeviceInfo(state);
        }

        public bool IsWorking(FiscalPrinterState state)
        {
            return GetPrinter(state.Driver).IsWorking(state);
        }

        public void PrintAndCloseSale(FiscalPrinterState state, Sale sale)
        {
            GetPrinter(state.Driver).PrintAndCloseSale(state, sale);
        }

        public void PrintDailyReport(FiscalPrinterState state)
        {
            GetPrinter(state.Driver).PrintDailyReport(state);
        }

        public void WithdrawMoney(FiscalPrinterState state, decimal sum)
        {
            GetPrinter(state.Driver).WithdrawMoney(state, sum);
        }
    }
}