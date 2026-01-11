using UssdDevelopmentCore.Common;

namespace UssdDevelopmentCore.Models;

[PostgresEnum]
public enum VoucherStatus
{
    Open,
    Closed
}

public class VoucherTransaction : BaseEntity
{
    public required int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    
    public required int WalletTransactionId { get; set; }
    public WalletTransaction WalletTransaction { get; set; } = null!;
    
    public required string Recipient { get; set; }
    public required string Code { get; set; }
    public VoucherStatus VoucherStatus { get; set; } = VoucherStatus.Open;
}