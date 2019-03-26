namespace ErpNet.FP.Print.Core
{
    /// <summary>
    /// Payment type. The printer should be appropriately configured.
    /// </summary>
    public enum PaymentType
    {
        Cash = 0,
        BankTransfer = 1,
        DebitCard = 2,
        NationalHealthInsuranceFund = 3,
        Voucher = 4,
        Coupon = 5
    }
}
