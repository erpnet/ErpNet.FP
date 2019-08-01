using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ErpNet.FP.Core.Drivers
{
    /// <summary>
    /// Fiscal printer base class for Bg printers.
    /// </summary>
    /// <seealso cref="ErpNet.FP.IFiscalPrinter" />
    public abstract class BgFiscalPrinter : IFiscalPrinter
    {
        public DeviceInfo DeviceInfo => Info;
        protected IDictionary<string, string> Options { get; }
        protected IChannel Channel { get; }

        public DeviceInfo Info = new DeviceInfo();

        protected static readonly object frameSyncLock = new object();

        protected Encoding PrinterEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1251);

        protected BgFiscalPrinter(IChannel channel, IDictionary<string, string>? options = null)
        {
            Options = new Dictionary<string, string>()
                .MergeWith(GetDefaultOptions())
                .MergeWith(options);
            Channel = channel;
        }

        public virtual IDictionary<string, string>? GetDefaultOptions()
        {
            return null;
        }

        public abstract string GetTaxGroupText(TaxGroup taxGroup);

        public abstract string GetPaymentTypeText(PaymentType paymentType);

        public virtual string GetReversalReasonText(ReversalReason reversalReason)
        {
            switch (reversalReason)
            {
                case ReversalReason.OperatorError:
                    return "0";
                case ReversalReason.Refund:
                    return "1";
                case ReversalReason.TaxBaseReduction:
                    return "2";
                default:
                    return "0";
            }
        }

        public abstract DeviceStatusWithDateTime CheckStatus();

        public abstract DeviceStatusWithCashAmount Cash();

        public abstract DeviceStatus SetDateTime(CurrentDateTime currentDateTime);

        public abstract DeviceStatus PrintMoneyDeposit(TransferAmount transferAmount);

        public abstract DeviceStatus PrintMoneyWithdraw(TransferAmount transferAmount);

        public abstract (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt);

        public abstract DeviceStatus PrintReversalReceipt(ReversalReceipt reversalReceipt);

        public abstract DeviceStatus PrintZReport(Credentials credentials);

        public abstract DeviceStatus PrintXReport(Credentials credentials);

        public abstract DeviceStatusWithRawResponse RawRequest(RequestFrame requestFrame);

        protected abstract DeviceStatus ParseStatus(byte[]? status);        

        public virtual DeviceStatus ValidateReceipt(Receipt receipt)
        {
            var status = new DeviceStatus();
            if (receipt.Items == null || receipt.Items.Count == 0)
            {
                status.AddError("E410", "Receipt is empty, no items");
                return status;
            }
            if (String.IsNullOrEmpty(receipt.UniqueSaleNumber))
            {
                status.AddError("E405", "UniqueSaleNumber is empty");
                return status;
            }
            var uniqueSaleNumberMatch = Regex.Match(receipt.UniqueSaleNumber, "^[A-Z0-9]{8}-[A-Z0-9]{4}-[0-9]{7}$");
            if (!uniqueSaleNumberMatch.Success)
            {
                status.AddError("E405", "Invalid format of UniqueSaleNumber");
                return status;
            }
            var itemsTotalAmount = 0.00m;
            var row = 0;
            foreach (var item in receipt.Items)
            {
                row++;
                if (String.IsNullOrEmpty(item.Text))
                {
                    status.AddError("E407", $"Item {row}: \"text\" is empty");
                }

                // Validation of "type" : "sale"
                if (item.Type == ItemType.Sale)
                {
                    if (item.PriceModifierValue <= 0 && item.PriceModifierType != PriceModifierType.None)
                    {
                        status.AddError("E403", $"Item {row}: \"priceModifierValue\" should be positive number");
                    }
                    if (item.PriceModifierValue != 0 && item.PriceModifierType == PriceModifierType.None)
                    {
                        status.AddError("E403", $"Item {row}: \"priceModifierValue\" should'nt be \"none\" or empty. You can avoid setting priceModifier if you do not want price modification");
                    }
                    if (item.Quantity <= 0)
                    {
                        status.AddError("E403", $"Item {row}: \"quantity\" should be positive number");
                    }
                    if (item.TaxGroup == TaxGroup.Unspecified)
                    {
                        status.AddError("E403", $"Item {row}: \"taxGroup\" should'nt be \"unspecified\" or empty");
                    }
                    try
                    {
                        GetTaxGroupText(item.TaxGroup);
                    }
                    catch (StandardizedStatusMessageException e)
                    {
                        status.AddError(e.Code, e.Message);
                    }
                    var quantity = Math.Round(item.Quantity, 3, MidpointRounding.AwayFromZero);
                    var unitPrice = Math.Round(item.UnitPrice, 2, MidpointRounding.AwayFromZero);
                    var itemPrice = quantity * unitPrice;
                    switch (item.PriceModifierType)
                    {
                        case PriceModifierType.DiscountAmount:
                            itemPrice -= item.PriceModifierValue;
                            break;
                        case PriceModifierType.DiscountPercent:
                            itemPrice -= itemPrice * (item.PriceModifierValue / 100.0m);
                            break;
                        case PriceModifierType.SurchargeAmount:
                            itemPrice += item.PriceModifierValue;
                            break;
                        case PriceModifierType.SurchargePercent:
                            itemPrice += itemPrice * (item.PriceModifierValue / 100.0m);
                            break;
                    }
                    itemsTotalAmount += Math.Round(itemPrice, 2, MidpointRounding.AwayFromZero);
                }

                if (!status.Ok)
                {
                    return status;
                }
            }
            if (receipt.Payments?.Count > 0)
            {
                var paymentAmount = 0.00m;
                row = 0;
                foreach (var payment in receipt.Payments)
                {
                    row++;
                    if (payment.Amount <= 0)
                    {
                        status.AddError("E403", $"Payment {row}: \"amount\" should be positive number");
                    }
                    try
                    {
                        GetPaymentTypeText(payment.PaymentType);
                    }
                    catch (StandardizedStatusMessageException e)
                    {
                        status.AddError(e.Code, e.Message);
                    }
                    if (!status.Ok)
                    {
                        status.AddInfo($"Error occured at Payment {row}");
                        return status;
                    }
                    paymentAmount += payment.Amount;
                }
                var difference = Math.Abs(paymentAmount - itemsTotalAmount);
                if (difference >= 0.01m && difference / itemsTotalAmount > 0.00001m)
                {
                    status.AddError("E403", $"Payment total amount ({paymentAmount.ToString(CultureInfo.InvariantCulture)}) should be the same as the items total amount ({itemsTotalAmount.ToString(CultureInfo.InvariantCulture)})");
                }
            }
            return status;
        }

        public virtual DeviceStatus ValidateReversalReceipt(ReversalReceipt reversalReceipt)
        {
            var status = ValidateReceipt(reversalReceipt);
            if (!status.Ok)
            {
                return status;
            }
            if (String.IsNullOrEmpty(reversalReceipt.ReceiptNumber))
            {
                status.AddError("E405", $"ReceiptNumber of the original receipt is empty");
                return status;
            }
            if (String.IsNullOrEmpty(reversalReceipt.FiscalMemorySerialNumber))
            {
                status.AddError("E405", $"FiscalMemorySerialNumber of the original receipt is empty");
                return status;
            }
            return status;
        }

       

        public virtual DeviceStatus ValidateTransferAmount(TransferAmount transferAmount)
        {
            var status = new DeviceStatus();
            if (transferAmount.Amount <= 0)
            {
                status.AddError("E403", "Amount should be positive number");
            }
            return status;
        }

        protected virtual string WithPrinterEncoding(string text)
        {
            return PrinterEncoding.GetString(
                Encoding.Convert(Encoding.Default, PrinterEncoding, Encoding.Default.GetBytes(text)));
        }
    }
}
