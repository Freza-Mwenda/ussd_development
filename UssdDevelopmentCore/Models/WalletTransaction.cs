using UssdDevelopmentCore.Common;

namespace UssdDevelopmentCore.Models;

[PostgresEnum]
public enum TransactionStatus
{
    Pending,
    Successful,
    Failed
}

[PostgresEnum]
public enum TransactionType
{
    Deposit,
    Withdraw,
    WalletToWallet,
    WalletToMobileMoney
}


public class WalletTransaction : BaseEntity
{
    public required int MemberId { get; set; }
    public Member? Member { get; set; } = null!;
    
    public string ExternalTxnId { get; set; } = string.Empty;
    public required string TransactionId { get; set; }
    public required string BilledAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public TransactionType Type { get; set; } = TransactionType.Deposit;
}

