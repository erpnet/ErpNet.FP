namespace ErpNet.FP.Core.Service
{
    using System;

    public enum PrintJobAction
    {
        None,
        Cash,
        RawRequest,
        Receipt,
        ReversalReceipt,
        Withdraw,
        Deposit,
        XReport,
        ZReport,
        SetDateTime,
        Reset
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
        public string? TaskId;
        public int AsyncTimeout = DefaultTimeout;

        public void Run()
        {
            if (Printer == null) return;
            Started = DateTime.Now;
            TaskStatus = TaskStatus.Running;
            try
            {
                switch (Action)
                {
                    case PrintJobAction.Cash:
                        Result = Printer.Cash((Credentials)(Document ?? new Credentials()));
                        break;
                    case PrintJobAction.RawRequest:
                        if (Document != null)
                        {
                            Result = Printer.RawRequest((RequestFrame)Document);
                        }
                        break;
                    case PrintJobAction.Receipt:
                        if (Document != null)
                        {
                            var receipt = (Receipt)Document;
                            var validateStatus = Printer.ValidateReceipt(receipt);
                            if (validateStatus.Ok)
                            {
                                var (info, status) = Printer.PrintReceipt(receipt);
                                Result = new DeviceStatusWithReceiptInfo(status, info);
                            }
                            else
                            {
                                Result = validateStatus;
                            }
                        }
                        break;
                    case PrintJobAction.ReversalReceipt:
                        if (Document != null)
                        {
                            var reversalReceipt = (ReversalReceipt)Document;
                            var validateStatus = Printer.ValidateReversalReceipt(reversalReceipt);
                            if (validateStatus.Ok)
                            {
                                var (info, status) = Printer.PrintReversalReceipt(reversalReceipt);
                                Result = new DeviceStatusWithReceiptInfo(status, info);
                            }
                            else
                            {
                                Result = validateStatus;
                            }
                        };
                        break;
                    case PrintJobAction.Withdraw:
                        if (Document != null)
                        {
                            var transferAmount = (TransferAmount)Document;
                            var validateStatus = Printer.ValidateTransferAmount(transferAmount);
                            if (validateStatus.Ok)
                            {
                                Result = Printer.PrintMoneyWithdraw(transferAmount);
                            }
                            else
                            {
                                Result = validateStatus;
                            }
                        }
                        break;
                    case PrintJobAction.Deposit:
                        if (Document != null)
                        {
                            var transferAmount = (TransferAmount)Document;
                            var validateStatus = Printer.ValidateTransferAmount(transferAmount);
                            if (validateStatus.Ok)
                            {
                                Result = Printer.PrintMoneyDeposit((TransferAmount)Document);
                            }
                            else
                            {
                                Result = validateStatus;
                            }
                        }
                        break;
                    case PrintJobAction.XReport:
                        Result = Printer.PrintXReport((Credentials)(Document ?? new Credentials()));
                        break;
                    case PrintJobAction.ZReport:
                        Result = Printer.PrintZReport((Credentials)(Document ?? new Credentials()));
                        break;
                    case PrintJobAction.SetDateTime:
                        if (Document != null)
                        {
                            Result = Printer.SetDateTime((CurrentDateTime)Document);
                        };
                        break;
                    case PrintJobAction.Reset:
                        Result = Printer.Reset((Credentials)(Document ?? new Credentials()));
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
