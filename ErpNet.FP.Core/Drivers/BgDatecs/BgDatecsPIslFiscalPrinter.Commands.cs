namespace ErpNet.FP.Core.Drivers.BgDatecs
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Fiscal printer using the ISL implementation of Datecs Bulgaria.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Drivers.BgIslFiscalPrinter" />
    public partial class BgDatecsPIslFiscalPrinter : BgIslFiscalPrinter
    {

        protected const byte
            CommandDatecsOpenReversalReceipt = 0x2e;
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
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.Password", "0000").WithMaxLength(Info.OperatorPasswordMaxLength)
                        :
                        operatorPassword,
                    "1",
                    uniqueSaleNumber
                });
            return Request(CommandOpenFiscalReceipt, header);
        }

        public override (string, DeviceStatus) AddItem(
            string itemText,
            decimal unitPrice,
            TaxGroup taxGroup,
            decimal quantity = 0,
            decimal priceModifierValue = 0,
            PriceModifierType priceModifierType = PriceModifierType.None)
        // Protocol [<L1>][<Lf><L2>]<Tab><TaxCd><[Sign]Price>[*<Qwan>][,Perc|;Abs]
        {
            var itemData = new StringBuilder()
                .Append(itemText.WithMaxLength(Info.ItemTextMaxLength))
                .Append('\t').Append(GetTaxGroupText(taxGroup))
                .Append(unitPrice.ToString("F2", CultureInfo.InvariantCulture));
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

        public override (string, DeviceStatus) GetTaxIdentificationNumber()
        {
            var (response, deviceStatus) = Request(CommandGetTaxIdentificationNumber);
            var commaFields = response.Split(',');
            if (commaFields.Length == 2)
            {
                return (commaFields[0].Trim(), deviceStatus);
            }
            return (string.Empty, deviceStatus);
        }

        public override string GetReversalReasonText(ReversalReason reversalReason)
        {
            return reversalReason switch
            {
                ReversalReason.OperatorError => "E",
                ReversalReason.Refund => "R",
                ReversalReason.TaxBaseReduction => "T",
                _ => "E",
            };
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
            // Protocol:<OpNum>,<Password>,<TillNum>[,<Invoice><InvNum>][,<UNP>],< StType >< DocNo >[,< StUNP >,< StDT >,< StFMIN >][#<StornoReason>]
            var header = string.Join(",",
                new string[] {
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.ID", "1")
                        :
                        operatorId,
                    String.IsNullOrEmpty(operatorId) ?
                        Options.ValueOrDefault("Operator.Password", "0000").WithMaxLength(Info.OperatorPasswordMaxLength)
                        :
                        operatorPassword,
                    "1",
                    GetReversalReasonText(reason)+receiptNumber,
                    uniqueSaleNumber,
                    receiptDateTime.ToString("ddMMyyHHmmss", CultureInfo.InvariantCulture),
                    fiscalMemorySerialNumber
                });

            return Request(CommandDatecsOpenReversalReceipt, header);
        }

        public override IDictionary<PaymentType, string> GetPaymentTypeMappings()
        {
            var paymentTypeMappings = new Dictionary<PaymentType, string> {
                { PaymentType.Cash,          "P" },
                { PaymentType.Check,         "C" },
                { PaymentType.Coupons,       "m" },
                { PaymentType.ExtCoupons,    "n" },
                { PaymentType.Packaging,     "o" },
                { PaymentType.InternalUsage, "p" },
                { PaymentType.Damage,        "q" },
                { PaymentType.Card,          "D" },
                { PaymentType.Bank,          "r" },
                { PaymentType.Reserved1,     "I" },
                { PaymentType.Reserved2,     "L" }
            };
            ServiceOptions.RemapPaymentTypes(Info.SerialNumber, paymentTypeMappings);
            return paymentTypeMappings;
        }

        // 6 Bytes x 8 bits
        protected static readonly (string?, string, StatusMessageType)[] StatusBitsStrings = new (string?, string, StatusMessageType)[] {
            // 0.0 # Получените данни имат синктактична грешка.
            ("E401", "Syntax error in the received data", StatusMessageType.Error),
            // 0.1 # Кодът на получената команда е невалиден.
            ("E402", "Invalid command code received", StatusMessageType.Error),
            // 0.2 Часовникът не е установен.
            ("E103", "The clock is not set", StatusMessageType.Error),
            // 0.3 Не е свързан клиентски дисплей.
            (null, "No customer display is connected", StatusMessageType.Info),
            // 0.4 # Механизмът на печатащото устройство има неизправност.
            ("E303", "Printing unit fault", StatusMessageType.Error),
            // 0.5 Обща грешка - това е OR на всички грешки, маркирани с ‘#’.
            ("E199", "General error", StatusMessageType.Error),
            // 0.6 Отворен е капакът на принтера.
            ("E302", "The printer cover is open", StatusMessageType.Error),
            // 0.7 Резервиран – винаги е 1.
            (null, string.Empty, StatusMessageType.Reserved),

            // 1.0 При изпълнение на командата се е получило препълване на някои полета от сумите. 
            ("E403", "The command resulted in an overflow of some amount fields", StatusMessageType.Error),
            // 1.1 # Изпълнението на командата не е позволено в текущия фискален режим.
            ("E404", "The command is not allowed in the current fiscal mode", StatusMessageType.Error),
            // 1.2 # Извършено е зануляване на оперативната памет.
            ("E104", "The RAM has been reset", StatusMessageType.Error),
            // 1.3 # Слаба батерия (Часовникът за реално време е в състояние RESET).
            ("E102", "Low battery (the real-time clock is in RESET status)", StatusMessageType.Error),
            // 1.4 Отворен сторно бон.
            (null, "A refund (storno) receipt is open", StatusMessageType.Info),
            // 1.5 Отворен е служебен бон за печат на завъртян на 90 градуса текст.
            (null, "A service receipt with 90-degree rotated text printing is open", StatusMessageType.Info),
            // 1.6 Вграденият данъчен терминал не отговаря.
            ("E599", "The built-in tax terminal is not responding", StatusMessageType.Error),
            // 1.7 Резервиран – винаги е 1.
            (null, string.Empty, StatusMessageType.Reserved),

            // 2.0 # Свършила е хартията. Ако се вдигне този флаг по време на команда, свързана с печат, то
            // командата е отхвърлена и не е променила състоянието на принтера.
            ("E301", "No paper", StatusMessageType.Error),
            // 2.1 Останала е малко хартия.
            ("W301", "Low paper", StatusMessageType.Warning),
            // 2.2 Край на КЛЕН (по-малко от 1 MB от КЛЕН свободни).
            ("E206", "End of the EJ", StatusMessageType.Error),
            // 2.3 Отворен е фискален бон.
            (null, "A fiscal receipt is open", StatusMessageType.Info),
            // 2.4 Близък край на КЛЕН (по-малко от 10 MB от КЛЕН свободни).
            ("W202", "The end of the EJ is near", StatusMessageType.Warning),
            // 2.5 Отворен е служебен бон.
            (null, "A service receipt is open", StatusMessageType.Info),
            // 2.6 Много близък край на КЛЕН (допускат се само определени бонове).
            ("W202", "The end of the EJ is very near", StatusMessageType.Warning),
            // 2.7 Резервиран – винаги е 1.
            (null, string.Empty, StatusMessageType.Reserved),

            // Byte 3, bits from 0 to 6 are SW 1 to 7
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            (null, string.Empty, StatusMessageType.Reserved),
            
            // 4.0 * Има грешка при запис във фискалната памет.
            ("E202", "Fiscal memory store error", StatusMessageType.Error),
            // 4.1 Зададен е ЕИК по БУЛСТАТ.
            (null, "BULSTAT UIC is set", StatusMessageType.Info),
            // 4.2 Зададени са индивидуален номер на принтера и номер на фискалната памет.
            (null, "Unique Printer ID and Fiscal Memory ID are set", StatusMessageType.Info),
            // 4.3 Има място за по-малко от 50 записа във ФП.
            ("W201", "There is space for less than 50 records remaining in the FP", StatusMessageType.Warning),
            // 4.4 * Фискалната памет е пълна.
            ("E201", "The fiscal memory is full", StatusMessageType.Error),
            // 4.5 OR на всички грешки, маркирани с ‘*’ от байтове 4 и 5.
            ("E299", "FM general error", StatusMessageType.Error),
            // 4.6 Печатащата глава е прегряла.
            ("E304", "The printing head is overheated", StatusMessageType.Error),
            // 4.7 Резервиран – винаги е 1.
            (null, string.Empty, StatusMessageType.Reserved),

            // 5.0 * Фискалната памет е установена в режим READONLY (заключена).
            ("E204", "The fiscal memory is set in READONLY mode (locked)", StatusMessageType.Error),
            // 5.1 Фискалната памет е форматирана.
            (null, "The fiscal memory is formatted", StatusMessageType.Info),
            // 5.2 * Последният запис във фискалната памет не е успешен.
            ("E202", "The last fiscal memory store operation is not successful", StatusMessageType.Error),
            //  5.3 Принтерът е във фискален режим.
            (null, "The printer is in fiscal mode", StatusMessageType.Info),
            // 5.4 Зададени са поне веднъж данъчните ставки.
            (null, "The tax rates are set at least once", StatusMessageType.Info),
            // 5.5 Грешка при четене от фискалната памет.
            ("E203", "Fiscal memory read error", StatusMessageType.Error),
            // 5.6 Не се използува.
            (null, string.Empty, StatusMessageType.Reserved),
            // 5.7 Резервиран – винаги е 1.
            (null, string.Empty, StatusMessageType.Reserved)
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
                byte mask = 0b10000000;
                byte b = status[i];
                // Byte 3 shows the switches SW1 .. SW7 state
                if (i == 3)
                {
                    var switchData = new List<string>();
                    // Skip bit 7
                    for (var j = 0; j < 7; j++)
                    {
                        mask >>= 1;
                        var switchState = ((mask & b) != 0) ? "ON" : "OFF";
                        switchData.Add($"SW{7 - j}={switchState}");
                    }
                    deviceStatus.AddInfo(string.Join(", ", switchData));
                }
                else
                {
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
            }
            return deviceStatus;
        }

    }
}
