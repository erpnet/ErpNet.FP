using ErpNet.FP.Core;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ErpNet.FP.WpfDemo.ViewModels
{
    class PrinterViewModel: ObservableObject
    {
        private readonly FiscalPrinterState state;

        public FiscalPrinterOperatorViewModel Operator { get; }
        public FiscalPrinterConnectionViewModel Connection { get; }

        public PrinterViewModel()
        {
            state = new FiscalPrinterState();
            Operator = new FiscalPrinterOperatorViewModel(state);
            Connection = new FiscalPrinterConnectionViewModel(state);
        }

        public ICommand ConnectToPrinterCommand { get; } = new RelayCommand(
            execute: () =>
            {

            },
            canExecute: () =>
            {
                return true;
            });
    }

    class FiscalPrinterConnectionViewModel: ObservableObject
    {
        private readonly FiscalPrinterState state;

        private bool isConnected;
        public bool IsConnected
        {
            get => isConnected;
            set => Set(ref isConnected, value);
        }

        public FiscalPrinterPort ComPort
        {
            get => state.ComPort;
            set => Set(ref state.ComPort, value);
        }

        public int BaudRate
        {
            get => state.BaudRate;
            set => Set(ref state.BaudRate, value);
        }

        public FiscalPrinterConnectionViewModel(FiscalPrinterState state)
        {
            this.state = state;
        }
    }

    class FiscalPrinterOperatorViewModel: ObservableObject
    {
        private readonly FiscalPrinterState state;

        private bool isLogged;
        public bool IsLogged
        {
            get => isLogged;
            set => Set(ref isLogged, value);
        }

        public string Name
        {
            get => state.Operator;
            set
            {
                if (Set(ref state.Operator, value))
                {
                    UpdateIsLogged();
                }
            }
        }

        public string Password
        {
            get => state.OperatorPassword;
            set
            {
                if (Set(ref state.OperatorPassword, value))
                {
                    UpdateIsLogged();
                }
            }
        }

        public FiscalPrinterOperatorViewModel(FiscalPrinterState state)
        {
            this.state = state;
        }

        private void UpdateIsLogged()
        {
            IsLogged = !string.IsNullOrEmpty(state.Operator) && !string.IsNullOrEmpty(state.OperatorPassword);
        }
    }
}
