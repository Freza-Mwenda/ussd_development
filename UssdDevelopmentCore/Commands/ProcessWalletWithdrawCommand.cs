using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;
using UssdDevelopmentCore.Services;
using UssdDevelopmentCore.Utilities;

namespace UssdDevelopmentCore.Commands;

public class ProcessWalletWithdrawCommand : ICommand<WalletResponse>
{
    public required decimal Amount { get; set; }
    public required string PhoneNumber { get; set; }
}

public class ProcessWalletWithdrawCommandHandler(IServiceScopeFactory factory) : ICommandHandler<ProcessWalletWithdrawCommand, WalletResponse>
{
    public async Task<WalletResponse> ExecuteAsync(ProcessWalletWithdrawCommand command, CancellationToken ct)
    {
        var database = factory.CreateScope().ServiceProvider.GetRequiredService<NasdacDatabase>();
        var generatorService = factory.CreateScope().ServiceProvider.GetRequiredService<IGeneratorService>();
        var nasdacUssdService = factory.CreateScope().ServiceProvider.GetRequiredService<INasdacUssdService>();

        var member = await database.Members
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.PhoneNumber, cancellationToken: ct);

        if (member == null)
        {
            throw new Exception("Member not found");
        }

        var txnId = await generatorService.TxnId();
        
        var walletTransaction = new WalletTransaction
        {
            MemberId = member.Id,
            ExternalTxnId = "",
            TransactionId = txnId,
            BilledAccount = command.PhoneNumber,
            Amount = command.Amount,
            Status = TransactionStatus.Pending,
            Type = TransactionType.Withdraw
        };
        
        await database.WalletTransactions.AddAsync(walletTransaction, ct);
        await database.SaveChangesAsync(ct);
        
        var result = await new ProcessLipilaWithdrawCommand
        {
            Currency = "ZMW",
            Amount = (double?)walletTransaction.Amount,
            AccountNumber = walletTransaction.BilledAccount,
            FullName = $"{member.FirstName} {member.LastName}",
            PhoneNumber = walletTransaction.BilledAccount,
            Email = "",
            ExternalId = walletTransaction.TransactionId,
            Narration = "Wallet withdraw",
        }.ExecuteAsync(ct: ct);
        
        walletTransaction.Status = result.Status;
        walletTransaction.ExternalTxnId = result.TxnId;

        if (result.Status == TransactionStatus.Successful)
        {
            var prevWallet = await database.Wallets
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.MemberId == member.Id, cancellationToken: ct);
            
            var newWalletRecord = new Wallet
            {
                MemberId = member.Id,
                WalletTransactionId = walletTransaction.Id,
                OpeningBalance = prevWallet!.ClosingBalance,
                ClosingBalance = prevWallet.ClosingBalance - walletTransaction.Amount,
                Type = TransactionType.Withdraw
            };
                
            BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                .SendSms(new Entities.SmsRequest
                {
                    Text = "Your Nasdac Global Money transaction was successful.",
                    Msisdn = command.PhoneNumber
                }));
                
            await database.Wallets.AddAsync(newWalletRecord, ct);
        }
        
        database.WalletTransactions.Update(walletTransaction);
        await database.SaveChangesAsync(ct);

        return new WalletResponse
        {
            MemberId = member.Id,
            Member = member.Adapt<ClientResponse>(),
            ExternalTxnId = walletTransaction.ExternalTxnId,
            TransactionId = walletTransaction.TransactionId,
            BilledAccount = walletTransaction.BilledAccount,
            Amount = walletTransaction.Amount,
            Status = walletTransaction.Status,
            Type = walletTransaction.Type
        };
    }
}