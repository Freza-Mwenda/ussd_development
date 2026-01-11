using System.Text.Json.Serialization;
using FastEndpoints;
using Flurl.Http;
using Microsoft.Extensions.Options;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Models;

namespace UssdDevelopmentCore.Commands;

public class SmsService
{
    public required string ApiUrl { get; set; }
}

public class SmsRequest
{
    [JsonPropertyName("originatorId")] public string OriginatorId { get; set; } = "NASDAC";
    [JsonPropertyName("msisdn")] public string Msisdn { get; set; } = null!;
    [JsonPropertyName("text")] public string Text { get; set; } = null!;
}

public class SendSmsCommand : ICommand<EmptyResponse>
{
    public required string Text { get; set; }
    public required string Msisdn { get; set; }
}

public class SendSmsCommandHandler(IServiceScopeFactory factory, IOptionsMonitor<SmsService> optionsMonitor, ILogger<SendSmsCommandHandler> logger) 
    : ICommandHandler<SendSmsCommand, EmptyResponse>
{ 
    private readonly SmsService _smsConnection = optionsMonitor.CurrentValue;
    
    public async Task<EmptyResponse> ExecuteAsync(SendSmsCommand command, CancellationToken ct)
    { 
        var dbContext = factory.CreateScope().ServiceProvider.GetRequiredService<NasdacDatabase>();  
        
        var response = await _smsConnection.ApiUrl.PostJsonAsync(new SmsRequest
        {
            OriginatorId = "NASDAC",
            Text = command.Text,
            Msisdn = command.Msisdn
        }, cancellationToken: ct);

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            logger.LogInformation("HTTP Status Code: {ResponseStatusCode}", response.StatusCode);
            logger.LogInformation("Error response: {ReadAsStringAsync}",
                await response.ResponseMessage.Content.ReadAsStringAsync(ct));
        }

        await dbContext.SmsMessages.AddAsync(new SmsMessage
        {
            Text = command.Text,
            Recipient = command.Msisdn,
            Sender = "NASDAC"
        }, ct);

        await dbContext.SaveChangesAsync(ct);

        var content = await response.ResponseMessage.Content.ReadAsStringAsync(ct);
        logger.LogInformation("Response content: {Content}", content);
        
        return await Task.FromResult(new EmptyResponse());
    }
}

