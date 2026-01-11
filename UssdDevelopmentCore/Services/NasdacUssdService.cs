using FastEndpoints;
using Hangfire;
using UssdDevelopmentCore.Commands;
using UssdDevelopmentCore.Entities;
using Entities_SmsRequest = UssdDevelopmentCore.Entities.SmsRequest;
using SmsRequest = UssdDevelopmentCore.Entities.SmsRequest;

namespace UssdDevelopmentCore.Services;

public interface INasdacUssdService
{
    Task ProcessWalletDeposit(WalletTransactionRequest request);
    Task ProcessWalletWithdraw(WalletTransactionRequest request);
    Task ProcessWalletToWalletTransfer(WalletToWalletRequest request);
    Task ProcessReceiveMoney(ReceiveMoneyRequest request);
    Task ProcessWalletToMobileMoney(WalletToMobileMoneyRequest request);
    Task SendSms(Entities_SmsRequest request);
}

public class NasdacUssdService(ILogger<NasdacUssdService> logger) : INasdacUssdService
{
    [Queue("a_priority")]
    public async Task ProcessWalletDeposit(WalletTransactionRequest request)
    {
        try
        {
            await new ProcessWalletDepositCommand
            {
                PhoneNumber = request.Msisdn,
                Amount = request.Amount
            }.ExecuteAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process wallet deposit.");
        }
    }
    
    [Queue("a_priority")]
    public async Task ProcessWalletWithdraw(WalletTransactionRequest request)
    {
        try
        {
            await new ProcessWalletWithdrawCommand
            {
                PhoneNumber = request.Msisdn,
                Amount = request.Amount
            }.ExecuteAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process wallet withdraw.");
        }
    }

    [Queue("a_priority")]
    public async Task SendSms(Entities_SmsRequest request)
    {
        try
        {
            await new SendSmsCommand
            {
                Text = request.Text,
                Msisdn = request.Msisdn
            }.ExecuteAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process sms");
        }
    }
    
    [Queue("a_priority")]
    public async Task ProcessWalletToWalletTransfer(WalletToWalletRequest request)
    {
        try
        {
            await new ProcessWalletToWalletCommand
            {
                Amount = request.Amount,
                SenderPhoneNumber = request.Sender,
                RecipientPhoneNumber = request.Receiver
            }.ExecuteAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"{e.Message}");
        }
    }
    
    [Queue("a_priority")]
    public async Task ProcessReceiveMoney(ReceiveMoneyRequest request)
    {
        try
        {
            await new ProcessReceiveMoneyCommand
            {
                VoucherId = request.VoucherId,
                Code = request.Code
            }.ExecuteAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"{e.Message}");
        }
    }
    
    [Queue("a_priority")]
    public async Task ProcessWalletToMobileMoney(WalletToMobileMoneyRequest request)
    {
        try
        {
            await new ProcessWalletToMobileMoneyCommand
            {
                Amount = request.Amount,
                SenderPhoneNumber = request.SenderPhoneNumber,
                RecipientPhoneNumber = request.RecipientPhoneNumber
            }.ExecuteAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"{e.Message}");
        }
    }
}