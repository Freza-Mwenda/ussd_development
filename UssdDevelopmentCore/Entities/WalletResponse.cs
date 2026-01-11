using UssdDevelopmentCore.Models;

namespace UssdDevelopmentCore.Entities;

public class WalletResponse
{
    public required int MemberId { get; set; }
    public ClientResponse? Member { get; set; } = null!;
    
    public string ExternalTxnId { get; set; }
    public required string TransactionId { get; set; }
    public required string BilledAccount { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public TransactionType Type { get; set; }
}