using ErpNet.FP.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TremolZFP;

namespace ErpNet.FP.Tremol.Zfp
{
    public class TremolZfpFiscalPrinter : IFiscalPrinter
    {
        private readonly Dictionary<PaymentType, OptionPaymentType> paymentTypesMap;

        public TremolZfpFiscalPrinter()
        {
            paymentTypesMap = new Dictionary<PaymentType, OptionPaymentType>();
        }

        /// <summary>Subce </summary>
        internal void Do(FiscalPrinterState state, Action<TremolZFP.FP> operation, [CallerMemberName] string callerMemberName = null)
        {
            TremolZFP.FP printer = null;
            try
            {
                if (GetType() != state.Driver)
                {
                    throw new FiscalPrinterDeviceTypeMismatchException($"Expected {GetType()}, got {state.Driver}");
                }

                string apiAdddress = string.IsNullOrEmpty(state.DriverApiAddress) ? "http://localhost:4444/" : state.DriverApiAddress;

                printer = new TremolZFP.FP() { ServerAddress = apiAdddress };
                
                // Note(Dilyan): samples suggest that we close before we open
                printer.ServerCloseDeviceConnection();

                // always attempt auto-discovery
                if (!printer.ServerFindDevice(out var serialPort, out var baudRate))
                {
                    serialPort = state.ComPort.ToString();
                    baudRate = state.BaudRate;
                }

                printer.ServerSetDeviceSerialPortSettings(serialPort, baudRate);

                if (paymentTypesMap.Count == 0)
                {
                    var paymentTypes = printer.ReadPayments();
                    FillPaymentTypes(paymentTypes);
                }

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


        public void Dispose()
        {
        }

        public bool IsWorking(FiscalPrinterState state)
        {
            bool isWorking = false;
            Do(state, printer =>
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
                catch (Exception /*ex*/)
                {
                    // TODO: log ex
                    isWorking = false;
                }
            });
            return isWorking;
        }

        public FiscalDeviceInfo GetDeviceInfo(FiscalPrinterState state)
        {
            FiscalDeviceInfo deviceInfo = new FiscalDeviceInfo();
            Do(state, printer => 
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

        public void DepositMoney(FiscalPrinterState state, decimal sum)
        {
            // I,1,______,_,__;{0};{1:0.00};;;;

            Do(state, printer =>
            {
                if (sum < 0) sum = -sum;

                printer.ReceivedOnAccount_PaidOut(
                        operNum: int.Parse(state.Operator),
                        operPass: state.OperatorPassword,
                        amount: sum,
                        text: "");
            });
        }

        public void WithdrawMoney(FiscalPrinterState state, decimal sum)
        {
            // I,1,______,_,__;{0};{1:0.00};;;;

            Do(state, printer =>
            {
                if (sum > 0) sum = -sum;

                printer.ReceivedOnAccount_PaidOut(
                        operNum: int.Parse(state.Operator),
                        operPass: state.OperatorPassword,
                        amount: sum,
                        text: "");
            });
        }

        public void PrintAndCloseSale(FiscalPrinterState state, Sale sale)
        {
            /*
             * 
             * 
             * string saleRowFormat = "S,1,______,_,__;{0};{1:0.00};{2:0.000};1;1;{3};0;0;\n";
             * string paymentRowFormat = "T,1,______,_,__;{0};{1:0.00};;;;\n";
             * 
             * SALE LINE
             * printerCommands.AppendFormat(CultureInfo.InvariantCulture, saleRowFormat, shortProductName, line.UnitPrice, line.Quantity, taxGroupCode);
             
             * PAYMENT INFO
             * printerCommands.AppendFormat(CultureInfo.InvariantCulture, paymentRowFormat, paymentTypeFlag, paymentAmount);
             * OR
             * printerCommands.Append("T,1,______,_,__;");
             */

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
                    printer.SellPLUwithSpecifiedVAT(
                        namePLU: GenerateProductName(line),
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
                throw new FiscalPrinterException($"Payment type {paymentType} not supported by {nameof(TremolZfpFiscalPrinter)}");
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

        internal static string GenerateProductName(SaleLine line)
        {
            const int maxProductNameLength = 35;

            var sb = new StringBuilder(line.ProductName.Length + line.ProductNumber.Length + 5);

            sb.Append(line.ProductNumber);
            sb.Append('|');
            sb.Append(line.ProductName);

            if (sb.Length > maxProductNameLength)
            {
                sb.Remove(maxProductNameLength, sb.Length - maxProductNameLength);
            }

            return sb.ToString();
        }

        
    }
}
