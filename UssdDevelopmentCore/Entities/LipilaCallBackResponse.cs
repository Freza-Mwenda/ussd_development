using System.Text.Json.Serialization;

namespace UssdDevelopmentCore.Entities;

public class LipilaCallBackResponse
{
    [JsonPropertyName("transactionId")] public string TransactionId { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("amount")] public double Amount { get; set; }

    [JsonPropertyName("currency")] public string? Currency { get; set; }

    [JsonPropertyName("accountNumber")] public string? AccountNumber { get; set; }

    [JsonPropertyName("paymentType")] public string? PaymentType { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName(("externalId"))] public string? ExternalId { get; set; }

    [JsonPropertyName("customer")] public Customer? Customer { get; set; }

    [JsonPropertyName("ipAddress")] public string? IpAddress { get; set; }
}