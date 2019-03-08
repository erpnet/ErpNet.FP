//using System;
//using System.Collections.Generic;
//using ErpNet.FP.Core;
//using ErpNet.FP.Tremol.Zfp;

//namespace ErpNet.FP.Print
//{
//    public enum FiscalPrinterTypes
//    {
//        TremolZfp
//    }

//    public class FiscalPrinterState
//    {
//        public FiscalPrinterTypes Type;
//        public FiscalPrinterPort ComPort;
//        public string OperatorName;
//        public string OperatorPassword;
//    }

//    public class FiscalPrinterFacade
//    {
//        private TremolZfpFiscalPrinter tremolPrinter;
//        private IFiscalPrinter fiscalPrinter;

//        public FiscalPrinterFacade()
//        {

//        }

//        //private IFiscalPrinter GetFiscalPrinterByName()
//        //{

//        //}

//        /// <summary>
//        /// Prints and closes a sale.
//        /// </summary>
//        /// <param name="sale">The sale.</param>
//        /// <exception cref="FiscalPrinterException">If there is an error while trying to print the sale</exception>
//        public void PrintAndCloseSale(FiscalPrinterState state, Sale sale)
//        {

//        }

//        /// <summary>
//        /// Prints a daily report.
//        /// </summary>
//        /// <exception cref="FiscalPrinterException">If there is an error while trying to print the daily report</exception>
//        public void PrintDailyReport();

//        /// <summary>
//        /// Put money in the cash register and print an invoice
//        /// </summary>
//        /// <param name="sum">Amount of money to deposit (add)</param>
//        void DepositMoney(decimal sum);

//        /// <summary>
//        /// Take money from the cash register and print an invoice about the transaction
//        /// </summary>
//        /// <param name="sum">Amount of money to withdraw (take away_</param>
//        void WithdrawMoney(decimal sum);

//        /// <summary>
//        /// Return model and version for the device
//        /// </summary>
//        /// <returns>Model and version for the device</returns>
//        /// <exception cref="FiscalPrinterException">If there is an error while trying to connect to the fiscal device</exception>
//        FiscalDeviceInfo GetDeviceInfo();

//        /// <summary>
//        /// Try to determine if the device is working or not. 
//        /// </summary>
//        /// <returns>True if the device is ready and operational, false on any error</returns>
//        bool IsWorking();
//    }
//}
