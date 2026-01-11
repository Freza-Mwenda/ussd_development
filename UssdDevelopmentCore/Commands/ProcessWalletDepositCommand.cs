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

public class ProcessWalletDepositCommand : ICommand<WalletResponse>
{
    public required decimal Amount { get; set; }
    public required string PhoneNumber { get; set; }
}

public class WalletDepositCommandHandler(IServiceScopeFactory factory) : ICommandHandler<ProcessWalletDepositCommand, WalletResponse>
{
    public async Task<WalletResponse> ExecuteAsync(ProcessWalletDepositCommand command, CancellationToken ct)
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
            Type = TransactionType.Deposit
        };
        
        await database.WalletTransactions.AddAsync(walletTransaction, ct);
        await database.SaveChangesAsync(ct);
        
        var result = await new ProcessLipilaDepositCommand
        {
            Currency = "ZMW",
            Amount = walletTransaction.Amount,
            AccountNumber = walletTransaction.BilledAccount,
            FullName = $"{member.FirstName} {member.LastName}",
            PhoneNumber = walletTransaction.BilledAccount,
            Email = "",
            ExternalId = walletTransaction.TransactionId,
            Narration = "Wallet deposit"
        }.ExecuteAsync(ct: ct);
        
        walletTransaction.Status = result.Status;
        walletTransaction.ExternalTxnId = result.TxnId;

        if (result.Status == TransactionStatus.Successful)
        {
            var prevWallet = await database.Wallets
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.MemberId == member.Id, cancellationToken: ct);
            
            if (prevWallet == null)
            {
                var initialWalletRecord = new Wallet
                {
                    MemberId = member.Id,
                    WalletTransactionId = walletTransaction.Id,
                    OpeningBalance = 0,
                    ClosingBalance = walletTransaction.Amount,
                    Type = TransactionType.Deposit
                };
                
                BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                    .SendSms(new Entities.SmsRequest
                    {
                        Text = "Your Transaction from Mobile Money to Nasdac Global Money was successful.",
                        Msisdn = command.PhoneNumber
                    }));
                
                await database.Wallets.AddAsync(initialWalletRecord, ct);
                await database.SaveChangesAsync(ct);
            }
            else
            {
                var newWalletRecord = new Wallet
                {
                    MemberId = member.Id,
                    WalletTransactionId = walletTransaction.Id,
                    OpeningBalance = prevWallet.ClosingBalance,
                    ClosingBalance = prevWallet.ClosingBalance + walletTransaction.Amount,
                    Type = TransactionType.Deposit
                };
                
                BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                    .SendSms(new Entities.SmsRequest
                    {
                        Text = "Your Transaction from Mobile Money to Nasdac Global Money was successful.",
                        Msisdn = command.PhoneNumber
                    }));
                
                await database.Wallets.AddAsync(newWalletRecord, ct);
                await database.SaveChangesAsync(ct);
            }
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