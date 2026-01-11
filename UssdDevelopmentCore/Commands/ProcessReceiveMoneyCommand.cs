using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;
using UssdDevelopmentCore.Services;

namespace UssdDevelopmentCore.Commands;

public class ProcessReceiveMoneyCommand : ICommand<WalletResponse>
{
    public required int VoucherId { get; set; }
    public required string Code { get; set; }
}

public class ProcessReceiveMoneyCommandHandler(IServiceScopeFactory factory)
    : ICommandHandler<ProcessReceiveMoneyCommand, WalletResponse>
{
    public async Task<WalletResponse> ExecuteAsync(ProcessReceiveMoneyCommand command, CancellationToken ct)
    {
        var database = factory.CreateScope().ServiceProvider.GetRequiredService<NasdacDatabase>();
        var nasdacUssdService = factory.CreateScope().ServiceProvider.GetRequiredService<INasdacUssdService>();

        var voucherTransaction = await database.VoucherTransactions
            .Include(x => x.WalletTransaction)
            .ThenInclude(x => x.Member)
            .FirstOrDefaultAsync(x => x.Id == command.VoucherId, cancellationToken: ct);

        if (voucherTransaction == null)
        {
            throw new Exception("Voucher transaction not found");
        }

        if (voucherTransaction.Code != command.Code)
        {
            throw new Exception("The provided voucher PIN is invalid");
        }

        var walletTransaction = voucherTransaction.WalletTransaction;
        var member = voucherTransaction.WalletTransaction.Member;
        
        var result = await new ProcessLipilaWithdrawCommand
        {
            Currency = "ZMW",
            Amount = (double?)walletTransaction.Amount,
            AccountNumber = voucherTransaction.Recipient,
            FullName = $"{member?.FirstName} {member?.LastName}",
            PhoneNumber = voucherTransaction.Recipient,
            Email = "",
            ExternalId = walletTransaction.TransactionId,
            Narration = "Wallet to Mobile Money",
        }.ExecuteAsync(ct: ct);
        
        walletTransaction.Status = result.Status;
        walletTransaction.ExternalTxnId = result.TxnId;

        if (result.Status == TransactionStatus.Successful)
        {
            var prevWallet = await database.Wallets
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.MemberId == member!.Id, cancellationToken: ct);
            
            var newWalletRecord = new Wallet
            {
                MemberId = member!.Id,
                WalletTransactionId = walletTransaction.Id,
                OpeningBalance = prevWallet!.ClosingBalance,
                ClosingBalance = prevWallet.ClosingBalance - walletTransaction.Amount,
                Type = TransactionType.WalletToMobileMoney
            };

            voucherTransaction.VoucherStatus = VoucherStatus.Closed;
                
            BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                .SendSms(new Entities.SmsRequest
                {
                    Text = $"{voucherTransaction.Recipient} has received K {walletTransaction.Amount} from your Nasdac Global " +
                           $"Money wallet to mobile money transaction.",
                    Msisdn = member.PhoneNumber
                })); 
            
            BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                .SendSms(new Entities.SmsRequest
                {
                    Text = $"You have received K {walletTransaction.Amount} from {member.FirstName.ToUpper()} " +
                           $"{member.LastName.ToUpper()}. Dial *488*3# for NASDAC Global Money.",
                    Msisdn = voucherTransaction.Recipient
                }));
                
            await database.Wallets.AddAsync(newWalletRecord, ct);
            database.VoucherTransactions.Update(voucherTransaction);
        }
        
        database.WalletTransactions.Update(walletTransaction);
        await database.SaveChangesAsync(ct);

        return new WalletResponse
        {
            MemberId = member!.Id,
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