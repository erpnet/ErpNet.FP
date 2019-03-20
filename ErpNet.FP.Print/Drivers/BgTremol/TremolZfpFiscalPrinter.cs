using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ErpNet.FP.Core;
using TremolZFP;

namespace ErpNet.FP.Drivers.BgTremol
{
    public class BgTremoZfpHttpOptions
    {
        /// <summary>Should end in /</summary>
        public string ServerAddress = "http://localhost:4444/";
        /// <summary>Should be COM1, COM2, COM3, ..., COM9</summary>
        public string ComPort;
        public int BaudRate = 9600;

        /// <summary>From 1 to 20</summary>
        public int OperatorNumber = 1;
        /// <summary>Operator password - up to 20 symbols</summary>
        public string OperatorPassword = "0";
    }

    public class BgTremolZfpHttpFiscalPrinter : IFiscalPrinter
    {
        private readonly Dictionary<PaymentType, OptionPaymentType> paymentTypesMap;
        private readonly BgTremoZfpHttpOptions options;
        private bool setupWasCalled;


        // address is COM1,COM2,COM3.. COM9
        // ip.
        public BgTremolZfpHttpFiscalPrinter(BgTremoZfpHttpOptions options)
        {
            paymentTypesMap = new Dictionary<PaymentType, OptionPaymentType>();
            this.options = options;
            setupWasCalled = false;
        }

        internal TremolZFP.FP GetPrinter()
        {
            var printer = new TremolZFP.FP() { ServerAddress = options.ServerAddress };

            // Note(Dilyan): samples suggest that we close before we open
            printer.ServerCloseDeviceConnection();

            // always attempt auto-discovery
            printer.ServerSetDeviceSerialPortSettings(options.ComPort, options.BaudRate);

            return printer;
        }

        /// <summary>Subce </summary>
        internal void Do(Action<TremolZFP.FP> operation, [CallerMemberName] string callerMemberName = null)
        {
            if (!setupWasCalled)
            {
                throw new NotSupportedException($"You must call {nameof(SetupPrinter)} first");
            }

            TremolZFP.FP printer = null;
            try
            {
                printer = GetPrinter();
                operation(printer);
            }
            catch (FiscalPrinterException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FiscalPrinterException($"Unexpected error while trying to execute {callerMemberName}", ex);
            }
            finally
            {
                if (printer != null)
                {
                    printer.ServerCloseDeviceConnection();
                }
            }
        }

        public DeviceInfo GetDeviceInfo()
        {
            DeviceInfo deviceInfo = new DeviceInfo();
            Do(printer =>
            {
                var version = printer.ReadVersion();
                // var stat = printer.ReadDetailedPrinterStatus();
                // var parameters = printer.ReadParameters();
                //parameters.POSNum
                var regInfo = printer.ReadRegistrationInfo();

                deviceInfo.Company = "Tremol";
                deviceInfo.FirmwareVersion = version.Version;
                deviceInfo.FiscalMemorySerialNumber = regInfo.UIC;
                deviceInfo.Model = version.Model;
                deviceInfo.SerialNumber = regInfo.NRARegistrationNumber;
                deviceInfo.Type = version.OptionDeviceType.ToString();
            });
            return deviceInfo;
        }

        public bool IsReady()
        {
            bool isWorking = false;
            Do(printer =>
            {
                try
                {
                    var status = printer.ReadStatus();

                    bool hasError =
                        status.DateTime_not_set
                        || status.DateTime_wrong
                        || status.Deregistered
                        || status.FM_error
                        || !status.FM_fiscalized
                        || status.FM_full
                        || status.Hardware_clock_error
                        || status.No_GPRS_Modem //??
                                                //|| (!isDemoDevice && status.No_GPRS_service)
                        || status.No_mobile_operator
                        || status.No_SIM_card
                        || status.Printer_not_ready_no_paper
                        || status.Printer_not_ready_overheat
                        || status.Reports_registers_Overflow
                        || status.SD_card_full
                        || status.Wrong_SD_card;

                    isWorking = !hasError;
                }
                catch (Exception)
                {
                    // TODO: log ex
                    isWorking = false;
                }
            });
            return isWorking;
        }

        public PrintInfo PrintMoneyDeposit(decimal amount)
        {
            PrintInfo info = new PrintInfo();
            Do(printer =>
            {
                if (amount < 0) amount = -amount;

                printer.ReceivedOnAccount_PaidOut(
                    operNum: options.OperatorNumber,
                    operPass: options.OperatorPassword,
                    amount: amount,
                    text: "");
                
                var r = printer.ReadLastReceiptNum();
                info.FiscalMemoryPosition = r.ToString();
            });
            return info;
        }

        public PrintInfo PrintMoneyWithdraw(decimal amount)
        {
            PrintInfo info = new PrintInfo();
            Do(printer =>
            {
                if (amount > 0) amount = -amount;

                printer.ReceivedOnAccount_PaidOut(
                    operNum: options.OperatorNumber,
                    operPass: options.OperatorPassword,
                    amount: amount,
                    text: "");
                
                var r = printer.ReadLastReceiptNum();
                info.FiscalMemoryPosition = r.ToString();
            });
            return info;
        }

        public PrintInfo PrintReceipt(Receipt receipt)
        {
            throw new NotImplementedException();
        }

        public PrintInfo PrintReversalReceipt(Receipt reversalReceipt)
        {
            throw new NotImplementedException();
        }

        public PrintInfo PrintZeroingReport()
        {
            throw new NotImplementedException();
        }

        public void SetupPrinter()
        {
            if (setupWasCalled) return;
            
            TremolZFP.FP printer = null;
            try
            {
                printer = GetPrinter();

                var paymentTypes = printer.ReadPayments();
                FillPaymentTypes(paymentTypes);
            }
            finally
            {
                if (printer != null)
                {
                    printer.ServerCloseDeviceConnection();
                    printer = null;
                }
            }
            
            setupWasCalled = true;
        }

        internal void FillPaymentTypes(PaymentsRes printerPayments)
        {
            var currentTypes = new Dictionary<OptionPaymentType, string>();
            currentTypes[OptionPaymentType.Payment_0] = printerPayments.NamePayment0;
            currentTypes[OptionPaymentType.Payment_1] = printerPayments.NamePayment1;
            currentTypes[OptionPaymentType.Payment_2] = printerPayments.NamePayment2;
            currentTypes[OptionPaymentType.Payment_3] = printerPayments.NamePayment3;
            currentTypes[OptionPaymentType.Payment_4] = printerPayments.NamePayment4;
            currentTypes[OptionPaymentType.Payment_5] = printerPayments.NamePayment5;
            currentTypes[OptionPaymentType.Payment_6] = printerPayments.NamePayment6;
            currentTypes[OptionPaymentType.Payment_7] = printerPayments.NamePayment7;
            currentTypes[OptionPaymentType.Payment_8] = printerPayments.NamePayment8;
            currentTypes[OptionPaymentType.Payment_9] = printerPayments.NamePayment9;
            currentTypes[OptionPaymentType.Payment_10] = printerPayments.NamePayment10;
            currentTypes[OptionPaymentType.Payment_11] = printerPayments.NamePayment11;

            foreach (var kvp in currentTypes)
            {
                switch (kvp.Value.Trim().ToLower())
                {
                    case "лева":
                        paymentTypesMap[PaymentType.Cash] = kvp.Key;
                        break;

                    case "карта":
                        paymentTypesMap[PaymentType.ByCard] = kvp.Key;
                        break;

                    case "талон":
                        paymentTypesMap[PaymentType.Tokens] = kvp.Key;
                        break;

                    case "чек":
                        paymentTypesMap[PaymentType.Check] = kvp.Key;
                        break;
                }
            }
        }

        internal OptionPaymentType PaymentTypeToPrinterPaymentType(PaymentType paymentType)
        {
            if (paymentTypesMap.TryGetValue(paymentType, out var result))
            {
                return result;
            }
            else
            {
                throw new FiscalPrinterException($"Payment type {paymentType} not supported by {nameof(BgTremolZfpHttpFiscalPrinter)}");
            }
        }

        internal static OptionVATClass TaxGroupToVatClass(TaxGroup taxGroup)
        {
            switch (taxGroup)
            {
                case TaxGroup.GroupA:
                    return OptionVATClass.VAT_Class_0;
                case TaxGroup.GroupB:
                    return OptionVATClass.VAT_Class_1;
                case TaxGroup.GroupC:
                    return OptionVATClass.VAT_Class_2;
                case TaxGroup.GroupD:
                    return OptionVATClass.VAT_Class_3;

                default: throw new FiscalPrinterException("Unknown tax group " + taxGroup);
            }
        }

        /*
                public void PrintAndCloseSale(FiscalPrinterState state, Sale sale)
                {
                    //  string saleRowFormat = "S,1,______,_,__;{0};{1:0.00};{2:0.000};1;1;{3};0;0;\n";
                    //  string paymentRowFormat = "T,1,______,_,__;{0};{1:0.00};;;;\n";

                    //  SALE LINE
                    //  printerCommands.AppendFormat(CultureInfo.InvariantCulture, saleRowFormat, shortProductName, line.UnitPrice, line.Quantity, taxGroupCode);

                    //  PAYMENT INFO
                    //  printerCommands.AppendFormat(CultureInfo.InvariantCulture, paymentRowFormat, paymentTypeFlag, paymentAmount);
                    //  OR
                    //  printerCommands.Append("T,1,______,_,__;");


                    Do(state, printer =>
                    {
                        printer.OpenReceipt(
                            operNum: int.Parse(state.Operator),
                            operPass: state.OperatorPassword,
                            optionReceiptFormat: OptionReceiptFormat.Brief,
                            optionPrintVAT: OptionPrintVAT.Yes,
                            optionFiscalRcpPrintType: OptionFiscalRcpPrintType.Postponed_printing,
                            uniqueReceiptNumber: sale.UniqueSaleNumber);

                        foreach (var line in sale.Lines)
                        {
                            var productName = line.ProductName;
                            if (productName.Length > 35)
                            {
                                productName = productName.Substring(0, 35);
                            }

                            printer.SellPLUwithSpecifiedVAT(
                                namePLU: productName,
                                optionVATClass: TaxGroupToVatClass(line.TaxGroup),
                                price: line.UnitPrice,
                                quantity: line.Quantity,
                                discAddP: null,
                                discAddV: null);
                        }

                        if (sale.PaymentInfoLines.Count == 1 && sale.PaymentInfoLines[0].Amount == 0m)
                        {
                            printer.PayExactSum(PaymentTypeToPrinterPaymentType(sale.PaymentInfoLines[0].Type));
                        }
                        else
                        {
                            foreach (var payment in sale.PaymentInfoLines)
                            {
                                printer.Payment(
                                    optionPaymentType: PaymentTypeToPrinterPaymentType(payment.Type),
                                    optionChange: OptionChange.Without_Change,
                                    amount: payment.Amount,
                                    optionChangeType: null);
                            }
                        }

                        foreach (var nonFiscalText in sale.NonFiscalLines)
                        {
                            printer.PrintText(nonFiscalText);
                        }

                        printer.CloseReceipt();
                    });

                }

                public void PrintDailyReport(FiscalPrinterState state)
                {
                    // "Z,1,______,_,__;1;;"
                    Do(state, printer =>
                    {
                        printer.PrintDailyReport(OptionZeroing.Zeroing);
                    });
                }

                


                //internal static string GenerateProductName(SaleLine line)
                //{
                //    const int maxProductNameLength = 35;

                //    var sb = new StringBuilder(line.ProductName.Length + line.ProductNumber.Length + 5);

                //    sb.Append(line.ProductNumber);
                //    sb.Append('|');
                //    sb.Append(line.ProductName);

                //    if (sb.Length > maxProductNameLength)
                //    {
                //        sb.Remove(maxProductNameLength, sb.Length - maxProductNameLength);
                //    }

                //    return sb.ToString();
                //}
         */

    }
}
