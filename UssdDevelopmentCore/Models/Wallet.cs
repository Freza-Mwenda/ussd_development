namespace UssdDevelopmentCore.Models;

public class Wallet : BaseEntity
{
    public required int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    
    public required int WalletTransactionId { get; set; }
    public WalletTransaction WalletTransaction { get; set; } = null!;
    
    public required decimal OpeningBalance { get; set; }
    public required decimal ClosingBalance { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Deposit;
}