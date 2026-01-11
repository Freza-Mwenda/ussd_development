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

public class ProcessWalletToMobileMoneyCommand : ICommand<WalletResponse>
{
    public required decimal Amount { get; set; }
    public required string SenderPhoneNumber { get; set; }
    public required string RecipientPhoneNumber { get; set; }
}

public class ProcessWalletToMobileMoneyCommandHandler(IServiceScopeFactory factory) : ICommandHandler<ProcessWalletToMobileMoneyCommand, WalletResponse>
{
    public async Task<WalletResponse> ExecuteAsync(ProcessWalletToMobileMoneyCommand command, CancellationToken ct)
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
        
        var txnId = await generatorService.TxnId();
        
        var walletTransaction = new WalletTransaction
        {
            MemberId = sender.Id,
            ExternalTxnId = "",
            TransactionId = txnId,
            BilledAccount = command.SenderPhoneNumber,
            Amount = command.Amount,
            Status = TransactionStatus.Pending,
            Type = TransactionType.WalletToMobileMoney
        };
        
        await database.WalletTransactions.AddAsync(walletTransaction, ct);
        await database.SaveChangesAsync(ct);
        
        var random = new Random();
        var voucherCode = random.Next(1000, 9999).ToString();

        var voucherTransaction = new VoucherTransaction
        {
            MemberId = walletTransaction.MemberId,
            WalletTransactionId = walletTransaction.Id,
            Recipient = command.RecipientPhoneNumber,
            Code = voucherCode,
            VoucherStatus = VoucherStatus.Open
        };
        
        await database.VoucherTransactions.AddAsync(voucherTransaction, ct);
        await database.SaveChangesAsync(ct);
        
        BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
            .SendSms(new Entities.SmsRequest
            {
                Text = $"{sender.FirstName.ToUpper()} {sender.LastName.ToUpper()} has sent you K{walletTransaction.Amount}. " +
                       $"Your Voucher PIN is ${voucherCode}. Use the Code to withdraw cash by dialing *488*3#.",
                Msisdn = command.RecipientPhoneNumber
            }));
        
        BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
            .SendSms(new Entities.SmsRequest
            {
                Text = $"You have sent K{walletTransaction.Amount} to {command.RecipientPhoneNumber}. " +
                       $"Your Voucher PIN is ${voucherCode}. The recipient should use the Code to withdraw cash by dialing *488*3#.",
                Msisdn = sender.PhoneNumber
            }));
        
        return new WalletResponse
        {
            MemberId = sender.Id,
            Member = sender.Adapt<ClientResponse>(),
            ExternalTxnId = walletTransaction.ExternalTxnId,
            TransactionId = walletTransaction.TransactionId,
            BilledAccount = walletTransaction.BilledAccount,
            Amount = walletTransaction.Amount,
            Status = walletTransaction.Status,
            Type = walletTransaction.Type
        };
    }
}