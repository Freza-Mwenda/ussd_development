using System.Text.Json.Serialization;
using FastEndpoints;
using Flurl.Http;
using Microsoft.Extensions.Options;
using UssdDevelopmentCore.Common;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;

namespace UssdDevelopmentCore.Commands;

public class ProcessLipilaDepositCommand : ICommand<PaymentResult>
{
    [JsonPropertyName("currency")] 
    public required string Currency { get; set; }
    
    [JsonPropertyName("amount")] 
    public required decimal Amount { get; set; }
    
    [JsonPropertyName("accountNumber")] 
    public required string AccountNumber { get; set; }
    
    [JsonPropertyName("fullName")] 
    public required string FullName { get; set; }
    
    [JsonPropertyName("phoneNumber")] 
    public required string PhoneNumber { get; set; }
    
    [JsonPropertyName("email")] 
    public required string Email { get; set; }
    
    [JsonPropertyName("externalId")] 
    public required string ExternalId { get; set; }
    
    [JsonPropertyName("narration")] 
    public required string Narration { get; set; }
}

public class ProcessLipilaDepositCommandHandler(IOptionsMonitor<LipilaConnection> optionsMonitor, IOptionsMonitor<Provider> providerOptionsMonitor) 
    : ICommandHandler<ProcessLipilaDepositCommand, PaymentResult>
{
    private readonly LipilaConnection _lipilaConnection = optionsMonitor.CurrentValue;
    private readonly Provider _provider = providerOptionsMonitor.CurrentValue;
    
    public async Task<PaymentResult> ExecuteAsync(ProcessLipilaDepositCommand command, CancellationToken ct)
    {
        try
        {
            // if (_provider.Name == Environment.Development) command.Amount = 1;
            
            var response = await $"{_lipilaConnection.BaseUrl}/transactions/mobile-money"
                .WithOAuthBearerToken(_lipilaConnection.ApiKey)
                .PostJsonAsync(command, cancellationToken: ct)
                .ReceiveJson<LipilaPaymentResponse>();

            return new PaymentResult
            {
                Message = "Payment initiated successfully, please check your phone to complete the payment",
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