using Hangfire;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace UssdDevelopmentCore.Services;

public interface IPaymentService
{
    Task ProcessCallback(LipilaCallBackResponse response);   
}


public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly NasdacDatabase _database;
    private readonly INasdacUssdService _nasdacUssdService;

    public PaymentService(ILogger<PaymentService> logger, NasdacDatabase database, INasdacUssdService nasdacUssdService)
    {
        _logger = logger;
        _database = database;
        _nasdacUssdService = nasdacUssdService;
    }

    [Queue("a_priority")]
    public async Task ProcessCallback(LipilaCallBackResponse response)
    {
        var transaction = await _database.WalletTransactions
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x => x.ExternalTxnId == response.TransactionId);
        
        if (transaction == null)
        {
            _logger.LogError("Error, Transaction Can Not Be Null");
            return;
        }
        
        if (response.Status == LipilaTransactionStatus.Failed)
        {
            transaction.Status = TransactionStatus.Failed;
            transaction.UpdatedAt = DateTimeOffset.UtcNow;
            _database.WalletTransactions.Update(transaction);
            await _database.SaveChangesAsync();
            return;
        }
        
        var prevWallet = await _database.Wallets
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.MemberId == transaction.MemberId);

        switch (transaction.Type)
        {
            case TransactionType.Deposit when prevWallet == null:
            {
                var initialWalletRecord = new Wallet
                {
                    MemberId = transaction.MemberId,
                    WalletTransactionId = transaction.Id,
                    OpeningBalance = 0,
                    ClosingBalance = transaction.Amount,
                    Type = TransactionType.Deposit
                };
                
                BackgroundJobExtension.TryEnqueue(() => _nasdacUssdService
                    .SendSms(new SmsRequest
                    {
                        Text = "Your Transaction from Mobile Money to Nasdac Global Money was successful.",
                        Msisdn = transaction.BilledAccount
                    }));
                
                await _database.Wallets.AddAsync(initialWalletRecord);
                await _database.SaveChangesAsync();
                break;
            }
            case TransactionType.Deposit:
            {
                var newWalletRecord = new Wallet
                {
                    MemberId = transaction.MemberId,
                    WalletTransactionId = transaction.Id,
                    OpeningBalance = prevWallet.ClosingBalance,
                    ClosingBalance = prevWallet.ClosingBalance + transaction.Amount,
                    Type = TransactionType.Deposit
                };
                
                BackgroundJobExtension.TryEnqueue(() => _nasdacUssdService
                    .SendSms(new SmsRequest
                    {
                        Text = "Your Transaction from Mobile Money to Nasdac Global Money was successful.",
                        Msisdn = transaction.BilledAccount
                    }));
                
                await _database.Wallets.AddAsync(newWalletRecord);
                await _database.SaveChangesAsync();
                break;
            }
            case TransactionType.Withdraw when prevWallet == null:
            {
                var initialWalletRecord = new Wallet
                {
                    MemberId = transaction.MemberId,
                    WalletTransactionId = transaction.Id,
                    OpeningBalance = 0,
                    ClosingBalance = transaction.Amount,
                    Type = TransactionType.Withdraw
                };
                
                BackgroundJobExtension.TryEnqueue(() => _nasdacUssdService
                    .SendSms(new SmsRequest
                    {
                        Text = "Your Nasdac Global Money transaction was successful.",
                        Msisdn = transaction.BilledAccount
                    }));
                
                await _database.Wallets.AddAsync(initialWalletRecord);
                await _database.SaveChangesAsync();
                break;
            }
            case TransactionType.Withdraw:
            {
                var newWalletRecord = new Wallet
                {
                    MemberId = transaction.MemberId,
                    WalletTransactionId = transaction.Id,
                    OpeningBalance = prevWallet.ClosingBalance,
                    ClosingBalance = prevWallet.ClosingBalance - transaction.Amount,
                    Type = TransactionType.Withdraw
                };
                
                BackgroundJobExtension.TryEnqueue(() => _nasdacUssdService
                    .SendSms(new SmsRequest
                    {
                        Text = "Your Nasdac Global Money transaction was successful.",
                        Msisdn = transaction.BilledAccount
                    }));
                
                await _database.Wallets.AddAsync(newWalletRecord);
                await _database.SaveChangesAsync();
                break;
            }
            case TransactionType.WalletToWallet when prevWallet == null:
            {
                var initialWalletRecord = new Wallet
                {
                    MemberId = transaction.MemberId,
                    WalletTransactionId = transaction.Id,
                    OpeningBalance = 0,
                    ClosingBalance = transaction.Amount,
                    Type = TransactionType.WalletToWallet
                };
                
                BackgroundJobExtension.TryEnqueue(() => _nasdacUssdService
                    .SendSms(new SmsRequest
                    {
                        Text = "Your Nasdac Global Money wallet to wallet transaction was successful.",
                        Msisdn = transaction.BilledAccount
                    }));
                
                await _database.Wallets.AddAsync(initialWalletRecord);
                await _database.SaveChangesAsync();
                break;
            }
            case TransactionType.WalletToWallet:
            {
                var newWalletRecord = new Wallet
                {
                    MemberId = transaction.MemberId,
                    WalletTransactionId = transaction.Id,
                    OpeningBalance = prevWallet.ClosingBalance,
                    ClosingBalance = prevWallet.ClosingBalance - transaction.Amount,
                    Type = TransactionType.WalletToWallet
                };
                
                BackgroundJobExtension.TryEnqueue(() => _nasdacUssdService
                    .SendSms(new SmsRequest
                    {
                        Text = "Your Nasdac Global Money wallet to wallet transaction was successful.",
                        Msisdn = transaction.BilledAccount
                    }));
                
                await _database.Wallets.AddAsync(newWalletRecord);
                await _database.SaveChangesAsync();
                break;
            }
            case TransactionType.WalletToMobileMoney:
            {
                var voucherTransaction = await _database.VoucherTransactions
                    .Include(x => x.WalletTransaction)
                    .ThenInclude(x => x.Member)
                    .Include(x => x.Member)
                    .FirstOrDefaultAsync(x => x.WalletTransactionId == transaction.Id);

                if (voucherTransaction == null)
                {
                    throw new Exception("Voucher transaction not found");
                }
                
                var newWalletRecord = new Wallet
                {
                    MemberId = transaction.MemberId,
                    WalletTransactionId = transaction.Id,
                    OpeningBalance = prevWallet!.ClosingBalance,
                    ClosingBalance = prevWallet.ClosingBalance - transaction.Amount,
                    Type = TransactionType.WalletToMobileMoney
                };

                voucherTransaction.VoucherStatus = VoucherStatus.Closed;
                
                BackgroundJobExtension.TryEnqueue(() => _nasdacUssdService
                    .SendSms(new SmsRequest
                    {
                        Text = $"{voucherTransaction.Recipient} has received K {transaction.Amount} from your Nasdac Global " +
                               $"Money wallet to mobile money transaction.",
                        Msisdn = voucherTransaction.Member.PhoneNumber
                    }));
            
                BackgroundJobExtension.TryEnqueue(() => _nasdacUssdService
                    .SendSms(new SmsRequest
                    {
                        Text = $"You have received K {transaction.Amount} from {voucherTransaction.Member.FirstName.ToUpper()} " +
                               $"{voucherTransaction.Member.LastName.ToUpper()}. Dial *488*3# for NASDAC Global Money.",
                        Msisdn = voucherTransaction.Recipient
                    }));
                
                await _database.Wallets.AddAsync(newWalletRecord);
                _database.VoucherTransactions.Update(voucherTransaction);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        transaction.UpdatedAt = DateTimeOffset.UtcNow;
        transaction.Status = TransactionStatus.Successful;
        transaction.ExternalTxnId = response.TransactionId;
        
        _database.WalletTransactions.Update(transaction);
        await _database.SaveChangesAsync();
    }
}