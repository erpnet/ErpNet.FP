namespace ErpNet.FP.Print.Core
{
    /// <summary>
    /// Payment type. The printer should be appropriately configured.
    /// </summary>
    public enum PaymentType
    {
        Cash = 0,
        ByCard = 1,
        BankTransfer = 2,
        Tokens = 3,
        Check = 4,
        UserDefined1 = 5,
        UserDefined2 = 6,
        UserDefined3 = 7
    }
}
