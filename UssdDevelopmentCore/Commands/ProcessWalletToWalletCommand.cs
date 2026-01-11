using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;
using UssdDevelopmentCore.Services;
using UssdDevelopmentCore.Utilities;

namespace UssdDevelopmentCore.Commands;

public class ProcessWalletToWalletCommand : ICommand<WalletResponse>
{
    public required decimal Amount { get; set; }
    public required string SenderPhoneNumber { get; set; }
    public required string RecipientPhoneNumber { get; set; }
}

public class ProcessWalletToWalletCommandHandler(IServiceScopeFactory factory) : ICommandHandler<ProcessWalletToWalletCommand, WalletResponse>
{
    public async Task<WalletResponse> ExecuteAsync(ProcessWalletToWalletCommand command, CancellationToken ct)
    {
        var database = factory.CreateScope().ServiceProvider.GetRequiredService<NasdacDatabase>();
        var generatorService = factory.CreateScope().ServiceProvider.GetRequiredService<IGeneratorService>();
        var nasdacUssdService = factory.CreateScope().ServiceProvider.GetRequiredService<INasdacUssdService>();
        
        var sender = await database.Members
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.SenderPhoneNumber, cancellationToken: ct);

        if (sender == null)
        {
            throw new Exception("Sender not found");
        }
        
        var recipient = await database.Members
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.RecipientPhoneNumber, cancellationToken: ct);

        if (recipient == null)
        {
            throw new Exception("Recipient not found");
        }
        
        var txnId = await generatorService.TxnId();
        
        var walletTransaction = new WalletTransaction
        {
            MemberId = sender.Id,
            ExternalTxnId = "",
            TransactionId = txnId,
            BilledAccount = command.SenderPhoneNumber,
            Amount = command.Amount,
            Status = TransactionStatus.Successful,
            Type = TransactionType.WalletToWallet
        };
        
        await database.WalletTransactions.AddAsync(walletTransaction, ct);
        await database.SaveChangesAsync(ct);
        
        var prevSenderWallet = await database.Wallets
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.MemberId == sender.Id, cancellationToken: ct);
        
        var prevReceiverWallet = await database.Wallets
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.MemberId == recipient.Id, cancellationToken: ct);
            
        if (prevSenderWallet != null)
        {
            var senderWalletRecord = new Wallet
            {
                MemberId = sender.Id,
                WalletTransactionId = walletTransaction.Id,
                OpeningBalance = prevSenderWallet.ClosingBalance,
                ClosingBalance = prevSenderWallet.ClosingBalance - walletTransaction.Amount,
                Type = TransactionType.WalletToWallet
            };
            
            await database.Wallets.AddAsync(senderWalletRecord, ct);

            if (prevReceiverWallet == null)
            {
                var initialReceiverWalletRecord = new Wallet
                {
                    MemberId = recipient.Id,
                    WalletTransactionId = walletTransaction.Id,
                    OpeningBalance = 0,
                    ClosingBalance = walletTransaction.Amount,
                    Type = TransactionType.WalletToWallet
                };
                
                await database.Wallets.AddAsync(initialReceiverWalletRecord, ct);
            }
            else
            {
                var receiverWalletRecord = new Wallet
                {
                    MemberId = recipient.Id,
                    WalletTransactionId = walletTransaction.Id,
                    OpeningBalance = prevReceiverWallet.ClosingBalance,
                    ClosingBalance = prevSenderWallet.ClosingBalance + walletTransaction.Amount,
                    Type = TransactionType.WalletToWallet
                };
                
                await database.Wallets.AddAsync(receiverWalletRecord, ct);
            }
            
            BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                .SendSms(new Entities.SmsRequest
                {
                    Text = "Your Nasdac Global Money wallet to wallet transaction was successful.",
                    Msisdn = sender.PhoneNumber
                }));
            
            BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                .SendSms(new Entities.SmsRequest
                {
                    Text = $"You have received ZMW {walletTransaction.Amount} in your NASDAC wallet from {sender.FirstName} " +
                           $"{sender.LastName}.",
                    Msisdn = recipient.PhoneNumber
                }));
            
            await database.SaveChangesAsync(ct);
        }

        return new WalletResponse
        {
            MemberId = walletTransaction.MemberId,
            ExternalTxnId = walletTransaction.ExternalTxnId,
            TransactionId = walletTransaction.TransactionId,
            BilledAccount = walletTransaction.BilledAccount,
            Amount = walletTransaction.Amount,
            Status = walletTransaction.Status,
            Type = walletTransaction.Type
        };
    }
}