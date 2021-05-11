namespace ErpNet.FP.Core.Drivers.BgIncotex
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Fiscal printer using the ISL implementation of Incotex.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgIncotexIslFiscalPrinter : BgIslFiscalPrinter
    {
        protected const byte
            IncotexCommandGetDeviceConstants = 0x80,
            IncotexCommandAbortFiscalReceipt = 0x82;


        public override (string, DeviceStatus) AbortReceipt()
        {
            return Request(IncotexCommandAbortFiscalReceipt);
        }

        public (string, DeviceStatus) GetRawDeviceConstants()
        {
            return Request(IncotexCommandGetDeviceConstants);
        }

        public override (string, DeviceStatus) GetTaxIdentificationNumber()
        {
            var (response, deviceStatus) = Request(CommandGetTaxIdentificationNumber);
            var commaFields = response.Split(',');
            if (commaFields.Length == 2)
            {
                return (commaFields[1].Trim(), deviceStatus);
            }
            return (string.Empty, deviceStatus);
        }

        public override string GetReversalReasonText(ReversalReason reversalReason)
        {
            switch (reversalReason)
            {
                case ReversalReason.Refund:
                    return "S";
                case ReversalReason.TaxBaseReduction:
                    return "V";
                case ReversalReason.OperatorError:
                default:
                    return "R";
            }
        }

        public override (string, DeviceStatus) GetFiscalMemorySerialNumber()
        {
            var (rawDeviceInfo, deviceStatus) = GetRawDeviceInfo();
            var fields = rawDeviceInfo.Split(',');
            if (fields != null && fields.Length > 5)
            {
                return (fields[5], deviceStatus);
            }
            else
            {
                deviceStatus.AddInfo($"Error occured while reading device info");
                deviceStatus.AddError("E409", "Wrong number of fields");
                return (string.Empty, deviceStatus);
            }
        }

        public override (string, DeviceStatus) OpenReceipt(
            string uniqueSaleNumber,
            string operatorId,
            string operatorPassword)
        {
            var header = string.Join(",",
                new string[] {
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.ID", "1")
                        :
                        operatorId,
                    uniqueSaleNumber,
                    "0"
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public override (string, DeviceStatus) OpenReversalReceipt(
            ReversalReason reason,
            string receiptNumber,
            System.DateTime receiptDateTime,
            string fiscalMemorySerialNumber,
            string uniqueSaleNumber,
            string operatorId,
            string operatorPassword)
        {
            // Protocol: <OpNum>,<UNP>,<RevDocNo>[,<F1>[<F2><RevInvoiceNo>,<dd-mm-yy hh:mm:ss>,origDevDMNo]]
            var headerData = new StringBuilder()
                .Append(
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.ID", "1")
                        :
                        operatorId
                )
                .Append(',')
                .Append(uniqueSaleNumber)
                .Append(',')
                .Append(receiptNumber)
                .Append(',')
                .Append(GetReversalReasonText(reason))
                .Append(GetReversalReasonText(reason))
                .Append('0')
                .Append(',')
                .Append(receiptDateTime.ToString("dd-MM-yy HH:mm:ss", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(fiscalMemorySerialNumber);

            return Request(CommandOpenFiscalReceipt, headerData.ToString());
        }

        public override (string, DeviceStatus) AddItem(
            int department,
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup,
            decimal quantity = 0,
            decimal priceModifierValue = 0,
            PriceModifierType priceModifierType = PriceModifierType.None,
            int ItemCode = 999)
        {
            var itemData = new StringBuilder();
            if (department <= 0) 
            {
                itemData
                    .Append(itemText.WithMaxLength(Info.ItemTextMaxLength))
                    .Append('\t').Append(GetTaxGroupText(taxGroup))
                    .Append(unitPrice.ToString("F2", CultureInfo.InvariantCulture));
            }
            else
            {
                itemData
                    .Append(itemText.WithMaxLength(Info.ItemTextMaxLength))
                    .Append('\t').Append(department).Append('\t')
                    .Append(unitPrice.ToString("F2", CultureInfo.InvariantCulture));
            }

            if (quantity != 0)
            {
                itemData
                    .Append('*')
                    .Append(quantity.ToString(CultureInfo.InvariantCulture));
            }
            if (priceModifierType != PriceModifierType.None)
            {
                itemData
                    .Append(
                        priceModifierType == PriceModifierType.DiscountPercent
                        ||
                        priceModifierType == PriceModifierType.SurchargePercent
                        ? ',' : ';')
                    .Append((
                        priceModifierType == PriceModifierType.DiscountPercent
                        ||
                        priceModifierType == PriceModifierType.DiscountAmount
                        ? -priceModifierValue : priceModifierValue).ToString("F2", CultureInfo.InvariantCulture));
            }
            return Request(CommandFiscalReceiptSale, itemData.ToString());
        }

        public override (string, DeviceStatus) FullPayment()
        {
            return Request(CommandFiscalReceiptTotal, "\t");
        }

        public override (string, DeviceStatus) GetRawDeviceInfo()
        {
            return Request(CommandGetDeviceInfo, "1");
        }

        public override IDictionary<PaymentType, string> GetPaymentTypeMappings()
        {
            var paymentTypeMappings = new Dictionary<PaymentType, string> {
                { PaymentType.Cash,       "P" },
                { PaymentType.Card,       "C" },
                { PaymentType.Check,      "N" },
                { PaymentType.Reserved1,  "D" }
            };
            ServiceOptions.RemapPaymentTypes(Info.SerialNumber, paymentTypeMappings);
            return paymentTypeMappings;
        }

        // 6 Bytes x 8 bits

        protected static readonly (string?, string, StatusMessageType)[] StatusBitsStrings = new (string?, string, StatusMessageType)[] {
            ("E401", "Syntax error", StatusMessageType.Error),
            ("E402", "Invalid command", StatusMessageType.Error),
            ("E103", "Date and time are not set", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            ("E199", "General error", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            /*
            0.7 = 1 Резервиран – винаги е 1.
            0.6 = 1 Резервиран.
            0.5 = 1 Обща грешка - това е OR на всички грешки, маркирани с ‘#’.
            0.4 = 1 Резервиран.
            0.3 = 1 Не се използва.
            0.2 = 1 Часовникът не е установен.
            0.1 = 1# Кодът на получената команда е невалиден.
            0.0 = 1# В получените данни има синтактична грешка.
            */

            (null, string.Empty, StatusMessageType.Reserved),
            ("E404", "Command not allowed in this mode", StatusMessageType.Error),
            ("E104", "Zeroed RAM", StatusMessageType.Error),
            ("E405", "Invoice range not set", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            ("E408", "3 times repeated wrong password", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            /*
            1.7 = 1 Резервиран – винаги е 1.
            1.6 = 1 Резервиран.
            1.5 = 1 Поредно въвеждане на 3 грешни пароли.
            1.4 = 1# Резервиран.
            1.3 = 1# Не е зададен диапазон на броене на брояча на фактурите.
            1.2 = 1# Оперативната памет е нулирана.
            1.1 = 1# Изпълнението на командата не е позволено.
            1.0 = 1 Резервиран.
            */

            ("E301", "No paper", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "Opened Fiscal Receipt", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "Opened Non-fiscal Receipt", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            /*
            2.7 = 1 Резервиран – винаги е 1.
            2.6 = 1 Не се използва.
            2.5 = 1 Отворен служебен бон.
            2.4 = 1 Резервиран
            2.3 = 1 Oтворен фискален бон.
            2.2 = 1# Резервиран
            2.1 = 1 Резервиран.
            2.0 = 1# Край на хартията.
            */

            // Byte 3 is special in Incotex, it contains error code, from bit 0 to bit 6
            // bit 7 is reserved
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),

            ("E202", "Error while writing to FM", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            ("E203", "Wrong record in FM", StatusMessageType.Error),
            ("W201", "FM almost full", StatusMessageType.Warning),
            ("E201", "FM full", StatusMessageType.Error),
            ("E299", "FM general error", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            /*
            4.7 = 1 Резервиран – винаги е 1.
            4.6 = 1 Резервиран.
            4.5 = 1 OR на всички грешки, маркирани с ‘*’ от байтове 4 и 5.
            4.4 = 1* Фискалната памет е пълна.
            4.3 = 1 Има място за по-малко от 50 записа във ФП.
            4.2 = 1 Грешен запис във ФП
            4.1 = 1 Не се използва.
            4.0 = 1* Грешка при запис във фискалната памет.
            */

            ("E204", "FM Read only", StatusMessageType.Error),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, "FM ready", StatusMessageType.Info),
            (null, "VAT groups are set", StatusMessageType.Info),
            (null, "Device S/N and FM S/N are set", StatusMessageType.Info),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved)
            /*
            5.7 = 1 Резервиран – винаги е 1.
            5.6 = 1 Резервиран.
            5.5 = 1 Програмирани са индивидуалните номера на ФП и на ФУ
            5.4 = 1 Записани са данъчни ставки във ФП.
            5.3 = 1 Фискалното устройство е фискализирано
            5.2 = 1* Не се използва.
            5.1 = 1 Резервиран.
            5.0 = 1* Фискалната памет е установена в режим READONLY.
            */
        };

        protected override DeviceStatus ParseStatus(byte[]? status)
        {
            var deviceStatus = new DeviceStatus();
            if (status == null)
            {
                return deviceStatus;
            }
            for (var i = 0; i < status.Length; i++)
            {
                // Byte 3 is special in Incotex, it contains error code, from bit 0 to bit 6
                // bit 7 is reserved, so we will clear it from errorCode.
                if (i == 3)
                {
                    byte errorCode = (byte)(status[i] & 0b01111111);
                    if (errorCode > 0)
                    {
                        deviceStatus.AddError("E999", $"Error code: {errorCode}, see Incotex Manual");
                    }
                    continue;
                }
                byte mask = 0b10000000;
                byte b = status[i];
                for (var j = 0; j < 8; j++)
                {
                    if ((mask & b) != 0)
                    {
                        var (statusBitsCode, statusBitsText, statusBitStringType) = StatusBitsStrings[i * 8 + (7 - j)];
                        deviceStatus.AddMessage(new StatusMessage
                        {
                            Type = statusBitStringType,
                            Code = statusBitsCode,
                            Text = statusBitsText
                        });
                    }
                    mask >>= 1;
                }
            }
            return deviceStatus;
        }

        public override (decimal?, DeviceStatus) GetReceiptAmount()
        {
            decimal? receiptAmount = null;

            var (receiptStatusResponse, deviceStatus) = Request(CommandGetReceiptStatus, "T");
            if (!deviceStatus.Ok)
            {
                deviceStatus.AddInfo($"Error occured while reading last receipt status");
                return (null, deviceStatus);
            }

            var fields = receiptStatusResponse.Split(',');
            if (fields.Length < 4)
            {
                deviceStatus.AddInfo($"Error occured while parsing last receipt status");
                deviceStatus.AddError("E409", "Wrong format of receipt status");
                return (null, deviceStatus);
            }

            try
            {
                var amountString = fields[3];
                if (amountString.Length > 0)
                {
                    switch (amountString[0])
                    {
                        case '+':
                            receiptAmount = decimal.Parse(amountString.Substring(1), CultureInfo.InvariantCulture) / 100m;
                            break;
                        case '-':
                            receiptAmount = -decimal.Parse(amountString.Substring(1), CultureInfo.InvariantCulture) / 100m;
                            break;
                        default:
                            if (amountString.Contains("."))
                            {
                                receiptAmount = decimal.Parse(amountString, CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                receiptAmount = decimal.Parse(amountString, CultureInfo.InvariantCulture) / 100m;
                            }
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                deviceStatus = new DeviceStatus();
                deviceStatus.AddInfo($"Error occured while parsing the amount of last receipt status");
                deviceStatus.AddError("E409", e.Message);
                return (null, deviceStatus);
            }

            return (receiptAmount, deviceStatus);
        }

    }
}
