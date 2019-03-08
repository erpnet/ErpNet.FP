using ErpNet.FP.Core;
using FP3530;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ErpNet.FP.Datecs.ComServer
{
    public class DatecsComServerFiscalPrinter : IFiscalPrinter
    {
        public DatecsComServerFiscalPrinter()
        {
        }

        private void Do(
            Action<ICFD_BGR> action,
            [CallerMemberName] string actionName = null)
        {
            ICFD_BGR comClass = null;
            try
            {
                comClass = new CFD_BGRClass();
                comClass.set_TransportType(TTransportProtocol.ctc_RS232);
                comClass.set_RS232(6, 9600);
                comClass.OPEN_CONNECTION();
                action(comClass);
            }
            catch (FiscalPrinterException)
            {
                throw;
            }
            catch (Exception error)
            {
                throw new FiscalPrinterException($"Exception thrown while executing {actionName}", error);
            }
            finally
            {
                if (comClass != null)
                {
                    Exception error = null;
                    try
                    {
                        comClass.CLOSE_CONNECTION();
                        comClass.DestroyInstance();
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    Marshal.ReleaseComObject(comClass);
                    comClass = null;

                    if (error != null)
                    {
                        throw new FiscalPrinterException("Error while closing connection", error);
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        public FiscalDeviceInfo GetDeviceInfo()
        {
            FiscalDeviceInfo info;

            Do(device =>
            {
                info.Model = device.device_Number_Serial;
                info.Version = device.device_Model_Name;
            });
        }

        public bool IsWorking()
        {
            try
            {
                bool isWorking = false;
                Do(device =>
                {
                    isWorking = 
                        device.can_OpenFiscalReceipt
                        && device.can_OpenInvoiceReceipt
                        && device.can_OpenStornoReceipt
                        && (device.connected_ToDevice || device.connected_ToLAN);
                });
                return isWorking;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void DepositMoney(decimal sum)
        {
            Do(device =>
            {
                
            });
        }

        public void WithdrawMoney(decimal sum)
        {
            //?
            //printer.SetServiceMoney()
            throw new NotImplementedException();
        }

        public void PrintAndCloseSale(Sale sale)
        {
            
            throw new NotImplementedException();
        }

        public void PrintDailyReport()
        {
            
            printer.DailyReport()
        }

        
    }
}
