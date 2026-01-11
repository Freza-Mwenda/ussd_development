using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;
using UssdDevelopmentCore.Services;

namespace UssdDevelopmentCore.Commands;

public class CreateClientCommand : ICommand<ClientResponse>
{
    public required string PhoneNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string WalletPin { get; set; }
}

public class CreateClientCommandHandler(IServiceScopeFactory factory) : ICommandHandler<CreateClientCommand, ClientResponse>
{
    public async Task<ClientResponse> ExecuteAsync(CreateClientCommand command, CancellationToken ct)
    {
        var dbContext = factory.CreateScope().ServiceProvider.GetRequiredService<NasdacDatabase>();
        var nasdacUssdService = factory.CreateScope().ServiceProvider.GetRequiredService<INasdacUssdService>();

        var existingClient = await dbContext.Members
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.PhoneNumber, cancellationToken: ct);
        
        var client = new Member
        {
            PhoneNumber = command.PhoneNumber,
            FirstName = command.FirstName,
            LastName = command.LastName,
            WalletPin = command.WalletPin
        };

        if (existingClient == null)
        {
            await dbContext.Members.AddAsync(client, ct);
            await dbContext.SaveChangesAsync(ct);
            
            BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                .SendSms(new Entities.SmsRequest
                {
                    Text = "Welcome to NASDAC Global Money. Your Money - Your Way!",
                    Msisdn = command.PhoneNumber
                }));
        }
        
        return client.Adapt<ClientResponse>();
    }
}