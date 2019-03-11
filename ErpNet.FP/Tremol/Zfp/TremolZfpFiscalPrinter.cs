using ErpNet.FP.Core;
using System;
using System.Collections.Generic;
using System.Text;
using TremolZFP;

namespace ErpNet.FP.Tremol.Zfp
{
    public class ZfpOperator
    {
        public int OperatorNo;
        public string OperatorPassword;

        public static readonly ZfpOperator Default = new ZfpOperator { OperatorNo = 1, OperatorPassword = "0" };
    }

    public class ZfpOperatorEventArgs : EventArgs
    {
        public ZfpOperator Operator;
    }

    public class ZfpStatusResEventArgs : EventArgs
    {
        public StatusRes Status;
    }

    public class TremolZfpFiscalPrinter : IFiscalPrinter
    {
        internal readonly bool isDemoDevice;
        internal readonly TremolZFP.FP printer;
        internal Dictionary<PaymentType, OptionPaymentType> paymentTypesMap;

        public event EventHandler<ZfpOperatorEventArgs> OperatorNeeded;
        public event EventHandler<ZfpStatusResEventArgs> StatusError;

        public TremolZfpFiscalPrinter(bool isDemoDevice)
            : this("http://localhost:4444/", null, null, isDemoDevice)
        { }

        public TremolZfpFiscalPrinter()
            : this(false)
        { }

        internal TremolZfpFiscalPrinter(string address, FiscalPrinterPort? comPort, int? baudRate, bool isDemoDevice)
        {
            try
            {
                this.isDemoDevice = isDemoDevice;
                paymentTypesMap = new Dictionary<PaymentType, OptionPaymentType>();
                printer = new TremolZFP.FP() { ServerAddress = address };

                string serialPort;
                int serialPortBaudRate;
                if (comPort.HasValue)
                {
                    serialPort = comPort.Value.ToString();
                    serialPortBaudRate = baudRate ?? 115200;
                }
                else
                {
                    if (!printer.ServerFindDevice(out serialPort, out serialPortBaudRate))
                    {
                        throw new FiscalPrinterException("COM port wasn't specified explicitly and we couldn't auto-discover fiscal device");
                    }
                }

                printer.ServerCloseDeviceConnection();
                printer.ServerSetDeviceSerialPortSettings(serialPort, serialPortBaudRate);

                var paymentTypes = printer.ReadPayments();
                FillPaymentTypes(paymentTypes);
            }
            catch (Exception inner)
            {
                throw new FiscalPrinterException("Failed to initialize printer", inner);
            }
        }

        public void Dispose()
        {
            if (printer != null)
            {
                printer.ServerCloseDeviceConnection();
            }
        }

        public bool IsWorking()
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
                    || (!isDemoDevice && status.No_GPRS_service)
                    || status.No_mobile_operator
                    || status.No_SIM_card
                    || status.Printer_not_ready_no_paper
                    || status.Printer_not_ready_overheat
                    || status.Reports_registers_Overflow
                    || status.SD_card_full
                    || status.Wrong_SD_card;

                return !hasError;
            }
            catch (Exception /*ex*/)
            {
                // TODO: log ex
                return false;
            }
        }

        public FiscalDeviceInfo GetDeviceInfo()
        {
            try
            {
                FiscalDeviceInfo info;

                var version = printer.ReadVersion();

                info.Model = version.Model;
                info.Version = version.Version;
                return info;
            }
            catch (Exception ex)
            {
                throw new FiscalPrinterException(ex.Message, ex);
            }
        }

        public void DepositMoney(decimal sum)
        {
            // I,1,______,_,__;{0};{1:0.00};;;;

            try
            {
                if (sum < 0) sum = -sum;

                var op = GetOperator();

                printer.ReceivedOnAccount_PaidOut(
                        operNum: op.OperatorNo,
                        operPass: op.OperatorPassword,
                        amount: sum,
                        text: "");
            }
            catch (Exception e)
            {
                throw new FiscalPrinterException(e.Message, e);
            }
        }

        public void WithdrawMoney(decimal sum)
        {
            // I,1,______,_,__;{0};{1:0.00};;;;

            try
            {
                if (sum > 0) sum = -sum;

                var op = GetOperator();

                printer.ReceivedOnAccount_PaidOut(
                        operNum: op.OperatorNo,
                        operPass: op.OperatorPassword,
                        amount: sum,
                        text: "");
            }
            catch (Exception e)
            {
                throw new FiscalPrinterException(e.Message, e);
            }
        }

        public void PrintAndCloseSale(Sale sale)
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

            try
            {
                const int operNum = 1;
                const string operatorPassword = "0";

                printer.OpenReceipt(
                    operNum: operNum,
                    operPass: operatorPassword,
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
                    printer.CashPayCloseReceipt();
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

                    printer.CloseReceipt();
                }
            }
            catch (Exception e)
            {
                throw new FiscalPrinterException(e.Message, e);
            }
        }

        public void PrintDailyReport()
        {
            // "Z,1,______,_,__;1;;"
            try
            {
                printer.PrintDailyReport(OptionZeroing.Zeroing);
            }
            catch (Exception ex)
            {
                throw new FiscalPrinterException(ex.Message, ex);
            }
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

        internal void ReportStatusError(StatusRes status)
        {
            var handler = StatusError;
            if (handler != null)
            {
                handler(this, new ZfpStatusResEventArgs() { Status = status });
            }
        }

        internal ZfpOperator GetOperator()
        {
            var posOperator = ZfpOperator.Default;
            var handler = OperatorNeeded;
            if (handler != null)
            {
                var args = new ZfpOperatorEventArgs();
                handler(this, args);

                if (args.Operator != null)
                {
                    posOperator = args.Operator;
                }
            }
            return posOperator;
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
