using ErpNet.FP.Core;
using ErpNet.FP.Server.Models;
using System;

namespace ErpNet.FP.Server.Contexts
{
    public enum PrintJobAction
    {
        None,
        Receipt,
        ReversalReceipt,
        Withdraw,
        Deposit,
        XReport,
        ZReport,
        SetDateTime
    }

    public delegate object Run(object document);

    public class PrintJob
    {
        public const int DefaultTimeout = 29000; // 29 seconds

        public DateTime Enqueued = DateTime.Now;
        public DateTime? Started = null;
        public DateTime? Finished = null;

        public PrintJobAction Action = PrintJobAction.None;
        public IFiscalPrinter? Printer;
        public TaskStatus TaskStatus = TaskStatus.Unknown;
        public object? Document;
        public object? Result;

        public void Run()
        {
            if (Printer == null) return;
            Started = DateTime.Now;
            TaskStatus = TaskStatus.Running;
            try
            {
                switch (Action)
                {
                    case PrintJobAction.Receipt:
                        if (Document != null)
                        {
                            var (info, status) = Printer.PrintReceipt((Receipt)Document);
                            Result = new DeviceStatusWithReceiptInfo(status, info);
                        }
                        break;
                    case PrintJobAction.ReversalReceipt:
                        if (Document != null)
                        {
                            Result = Printer.PrintReversalReceipt((ReversalReceipt)Document);
                        };
                        break;
                    case PrintJobAction.Withdraw:
                        if (Document != null)
                        {
                            Result = Printer.PrintMoneyWithdraw((TransferAmount)Document);
                        }
                        break;
                    case PrintJobAction.Deposit:
                        if (Document != null)
                        {
                            Result = Printer.PrintMoneyDeposit((TransferAmount)Document);
                        }
                        break;
                    case PrintJobAction.XReport:
                        if (Document == null)
                        {
                            Document = new Credentials();
                        }
                        Result = Printer.PrintXReport((Credentials)Document);
                        break;
                    case PrintJobAction.ZReport:
                        if (Document == null)
                        {
                            Document = new Credentials();
                        }
                        Result = Printer.PrintZReport((Credentials)Document);
                        break;
                    case PrintJobAction.SetDateTime:
                        if (Document != null)
                        {
                            Result = Printer.SetDateTime((CurrentDateTime)Document);
                        };
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                Finished = DateTime.Now;
                TaskStatus = TaskStatus.Finished;
            }
        }
    }
}
