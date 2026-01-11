namespace UssdDevelopmentCore.Entities;

public class WalletTransactionRequest
{
    public required decimal Amount { get; set; }
    public required string Msisdn { get; set; }
}

public class WalletToWalletRequest
{
    public required decimal Amount { get; set; }
    public required string Sender { get; set; }
    public required string Receiver { get; set; }
}

public class ReceiveMoneyRequest
{
    public required int VoucherId { get; set; }
    public required string Code { get; set; }
}

public class WalletToMobileMoneyRequest
{
    public required decimal Amount { get; set; }
    public required string SenderPhoneNumber { get; set; }
    public required string RecipientPhoneNumber { get; set; }
}