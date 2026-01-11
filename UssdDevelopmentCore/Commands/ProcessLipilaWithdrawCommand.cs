using System.Text.Json.Serialization;
using FastEndpoints;
using Flurl.Http;
using Microsoft.Extensions.Options;
using UssdDevelopmentCore.Common;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;

namespace UssdDevelopmentCore.Commands;

public class ProcessLipilaWithdrawCommand : ICommand<PaymentResult>
{
    [JsonPropertyName("currency")] public string Currency { get; set; } = "ZMW";

    [JsonPropertyName("amount")] public double? Amount { get; set; }

    [JsonPropertyName("accountNumber")] public string AccountNumber { get; set; }

    [JsonPropertyName("fullName")] public string FullName { get; set; }

    [JsonPropertyName("phoneNumber")] public string PhoneNumber { get; set; }

    [JsonPropertyName("email")] public string Email { get; set; }

    [JsonPropertyName("externalId")] public string ExternalId { get; set; }

    [JsonPropertyName("narration")] public string Narration { get; set; }
}

public class ProcessLipilaWithdrawCommandHandler(IOptionsMonitor<LipilaConnection> optionsMonitor, IOptionsMonitor<Provider> providerOptionsMonitor)
    : ICommandHandler<ProcessLipilaWithdrawCommand, PaymentResult>
{
    private readonly LipilaConnection _lipilaConnection = optionsMonitor.CurrentValue;
    private readonly Provider _provider = providerOptionsMonitor.CurrentValue;
    
    public async Task<PaymentResult> ExecuteAsync(ProcessLipilaWithdrawCommand command, CancellationToken ct)
    {
        try
        {
            // if (_provider.Name == Environment.Development) command.Amount = 1;

            var response = await $"{_lipilaConnection.BaseUrl}/transactions/mobile-money/disburse"
                .WithOAuthBearerToken(_lipilaConnection.ApiKey)
                .PostJsonAsync(command, cancellationToken: ct)
                .ReceiveJson<LipilaPaymentResponse>();

            return new PaymentResult
            {
                Message = response.Message,
                Status = response.Status switch
                {
                    LipilaTransactionStatus.Pending => TransactionStatus.Pending,
                    LipilaTransactionStatus.Success => TransactionStatus.Successful,
                    LipilaTransactionStatus.Failed => TransactionStatus.Failed,
                    _ => throw new ArgumentOutOfRangeException()
                },
                TxnId = response.TransactionId
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}