using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Entities;

namespace UssdDevelopmentCore.Commands;

public class GetWalletBalanceCommand : ICommand<WalletBalance>
{
    public required string PhoneNumber { get; set; }
}

public class GetWalletBalanceCommandHandler(IServiceScopeFactory factory) : ICommandHandler<GetWalletBalanceCommand, WalletBalance>
{
    public async Task<WalletBalance> ExecuteAsync(GetWalletBalanceCommand command, CancellationToken ct)
    {
        var database = factory.CreateScope().ServiceProvider.GetRequiredService<NasdacDatabase>();
        
        var member = await database.Members
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.PhoneNumber, cancellationToken: ct);

        if (member == null)
        {
            throw new Exception("Member not found");
        }
        
        var prevWallet = await database.Wallets
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.MemberId == member.Id, cancellationToken: ct);
        
        if (prevWallet == null)
        {
            return new WalletBalance
            {
                Balance = 0
            };
        }

        return new WalletBalance
        {
            Balance = prevWallet.ClosingBalance
        };
    }
}