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
            FiscalPrinterState state,
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

        public FiscalDeviceInfo GetDeviceInfo(FiscalPrinterState state)
        {
            FiscalDeviceInfo info = default(FiscalDeviceInfo);

            Do(state, device =>
            {
                info.Company = "Datecs";
                info.FirmwareVersion = device.device_Firmware_Revision;
                info.FiscalMemorySerialNumber = device.device_Number_FMemory;
                info.Model = device.device_Model_Name;
                info.SerialNumber = device.device_Number_Serial;
                info.Type = device.device_Type.ToString();
            });

            return info;
        }

        public bool IsWorking(FiscalPrinterState state)
        {
            try
            {
                bool isWorking = false;
                Do(state, printer =>
                {
                    isWorking = 
                        printer.can_OpenFiscalReceipt
                        && printer.can_OpenInvoiceReceipt
                        && printer.can_OpenStornoReceipt
                        && (printer.connected_ToDevice || printer.connected_ToLAN);
                });
                return isWorking;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void DepositMoney(FiscalPrinterState state, decimal sum)
        {
            Do(state, printer =>
            {
                throw new NotImplementedException();
            });
        }

        public void WithdrawMoney(FiscalPrinterState state, decimal sum)
        {
            Do(state, printer =>
            {
                throw new NotImplementedException();
            });
        }

        public void PrintAndCloseSale(FiscalPrinterState state, Sale sale)
        {
            Do(state, printer =>
            {
                throw new NotImplementedException();
            });
        }

        public void PrintDailyReport(FiscalPrinterState state)
        {
            Do(state, printer =>
            {
                throw new NotImplementedException();
            });
        }
    }
}
