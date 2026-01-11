using UssdDevelopmentCore.Models;

namespace UssdDevelopmentCore.Entities;

public class PaymentResult
{
    public required TransactionStatus Status { get; set; }
    public required string Message { get; set; }
    public required string TxnId { get; set; }
}