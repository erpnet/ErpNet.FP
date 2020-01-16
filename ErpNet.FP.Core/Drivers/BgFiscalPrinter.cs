namespace ErpNet.FP.Core.Drivers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using ErpNet.FP.Core.Configuration;

    /// <summary>
    /// Fiscal printer base class for Bg printers.
    /// </summary>
    /// <seealso cref="ErpNet.FP.IFiscalPrinter" />
    public abstract class BgFiscalPrinter : IFiscalPrinter
    {

        protected static readonly object frameSyncLock = new object();

        protected Encoding PrinterEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1251);

        public DeviceInfo Info = new DeviceInfo();

        public IDictionary<PaymentType, string> PaymentTypeMappings = new Dictionary<PaymentType, string>();

        protected BgFiscalPrinter(
            IChannel channel, 
            ServiceOptions serviceOptions, 
            IDictionary<string, string>? options = null)
        {
            ServiceOptions = serviceOptions;
            Options = new Dictionary<string, string>()
                .MergeWith(GetDefaultOptions())
                .MergeWith(options);            
            Channel = channel;
        }

        protected abstract DeviceStatus ParseStatus(byte[]? status);

        protected IChannel Channel { get; }
        protected IDictionary<string, string> Options { get; }
        public ServiceOptions ServiceOptions { get; }

        public abstract DeviceStatusWithCashAmount Cash(Credentials credentials);

        public abstract DeviceStatusWithDateTime CheckStatus();

        public virtual IDictionary<string, string>? GetDefaultOptions()
        {
            return null;
        }

        public abstract IDictionary<PaymentType, string> GetPaymentTypeMappings();

        public string GetPaymentTypeText(PaymentType paymentType)
        {
            if (PaymentTypeMappings.TryGetValue(paymentType, out string? value))
            {
                if (value != null)
                {
                    return value;
                }
            }
            throw new StandardizedStatusMessageException($"Payment type {paymentType} unsupported", "E406");
        }

        public virtual string GetReversalReasonText(ReversalReason reversalReason)
        {
            return reversalReason switch
            {
                ReversalReason.OperatorError => "0",
                ReversalReason.Refund => "1",
                ReversalReason.TaxBaseReduction => "2",
                _ => "0",
            };
        }

        public ICollection<PaymentType> GetSupportedPaymentTypes()
        {
            PaymentTypeMappings = GetPaymentTypeMappings();
            return PaymentTypeMappings.Keys;
        }

        public abstract string GetTaxGroupText(TaxGroup taxGroup);

        public abstract DeviceStatus PrintMoneyDeposit(TransferAmount transferAmount);

        public abstract DeviceStatus PrintMoneyWithdraw(TransferAmount transferAmount);

        public abstract (ReceiptInfo, DeviceStatus) PrintReceipt(Receipt receipt);

        public abstract (ReceiptInfo, DeviceStatus) PrintReversalReceipt(ReversalReceipt reversalReceipt);

        public abstract DeviceStatus PrintXReport(Credentials credentials);

        public abstract DeviceStatus PrintZReport(Credentials credentials);

        public abstract DeviceStatusWithRawResponse RawRequest(RequestFrame requestFrame);

        public abstract DeviceStatusWithDateTime Reset(Credentials credentials);

        public abstract DeviceStatus SetDateTime(CurrentDateTime currentDateTime);


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
            var uniqueSaleNumberMatch = Regex.Match(receipt.UniqueSaleNumber, "^[A-Z]{2}[0-9]{6}-[A-Z0-9]{4}-[0-9]{7}$");
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
                    if (item.Quantity < 0)
                    {
                        status.AddError("E403", $"Item {row}: \"quantity\" should be positive number");
                    }
                    if (item.Department < 0) 
                    {
                        status.AddError("E403", $"Item {row}; \"department\" should be positive number or zero.");
                    }
                    if (item.TaxGroup == TaxGroup.Unspecified)
                    {
                        status.AddError("E403", $"Item {row}: \"taxGroup\" shouldn't be \"unspecified\" or empty");
                    }
                    try
                    {
                        GetTaxGroupText(item.TaxGroup);
                    }
                    catch (StandardizedStatusMessageException e)
                    {
                        status.AddError(e.Code, e.Message);
                    }
                    var quantity = Math.Round(item.Quantity == 0m ? 1m : item.Quantity, 3, MidpointRounding.AwayFromZero);
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

                    if (payment.Amount < 0 && payment.PaymentType != PaymentType.Change)
                    {
                        status.AddError("E403", $"Payment {row}: \"amount\" should be positive number or zero");
                    }
                    if (payment.Amount >= 0 && payment.PaymentType == PaymentType.Change)
                    {
                        status.AddError("E403", $"Change {row}: \"amount\" should be negative number");
                    }

                    try
                    {
                        if (payment.PaymentType != PaymentType.Change)
                        {
                            // Check if the payment type is supported
                            GetPaymentTypeText(payment.PaymentType);
                        }
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

                    var amount = Math.Round(payment.Amount, 2, MidpointRounding.AwayFromZero);
                    paymentAmount += amount;
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
            if (reversalReceipt.Payments?.Count > 0)
            {
                status.AddWarning("W302", "Reversal receipt payments array should be empty. It will be ignored.");
                reversalReceipt.Payments.Clear();
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

        public DeviceInfo DeviceInfo => Info;
    }
}
