using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Commands;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;
using UssdDevelopmentCore.Services;
using UssdDevelopmentCore.Utilities;
using UssdStateMachine;
using UssdStateMachine.Display;
using UssdStateMachine.Inputs;
using UssdStateMachine.Sessions;

namespace UssdDevelopmentCore.Menus;

public class MainMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Welcome to TechPay:")
            .AddOption("1. Send Money")
            .AddOption("2. Receive Money")
            .AddOption("3. Manage Wallet")
            .AddOption("4. Cancel")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", o => o.NavigateTo<SendMoneyMenu>())
            .Case("2", o => o.NavigateTo<ReceiveMoneyMenu>())
            .Case("3", o => o.NavigateTo<ManageWalletMenu>())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}


public class SendMoneyMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Select:")
            .AddOption("1. Wallet to Wallet")
            .AddOption("2. Wallet to Mobile Money\n")
            .AddOption("Press p for previous menu or 0 to cancel")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", o => o.NavigateTo<WalletToWalletMenu>())
            .Case("2", o => o.NavigateTo<WalletToMobileMoneyMenu>())
            .Case("p", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}

public class WalletToWalletMenu(IPhoneNumberValidator validator) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Enter the recipient's phone number (09********/07********):")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                var validatorResponse = validator.AddCountryCode(value);
                
                if (!validatorResponse.IsValid)
                {
                    await options.CloseUssdSessionAsync("Invest K1 and above.");
                }
                
                await session.SaveAsync(SessionKeys.RecipientPhoneNumber, validatorResponse.PhoneNumber);
                return await options.NavigateTo<WalletToWalletAmountMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class WalletToWalletAmountMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Enter the amount to send:")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                await session.SaveAsync(SessionKeys.Amount, value);
                return await options.NavigateTo<WalletToWalletConfirmMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class WalletToWalletConfirmMenu(INasdacUssdService nasdacUssdService) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        var amount = await session.GetAsync<string>(SessionKeys.Amount);
        var recipient = await session.GetAsync<string>(SessionKeys.RecipientPhoneNumber);
        
        return await options.SetPrompt($"You are sending K{amount} to wallet on {recipient}")
            .AddOption("1. Confirm")
            .AddOption("2. Cancel\n")
            .AddOption("Press 0 for main menu or p for previous menu")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", async options =>
            {
                var recipient = await session.GetAsync<string>(SessionKeys.RecipientPhoneNumber);
                var amount = await session.GetAsync<string>(SessionKeys.Amount);
                
                var isNasdacMember = await new MemberEligibilityCommand
                {
                    PhoneNumber = recipient
                }.ExecuteAsync();
                
                if (!isNasdacMember)
                {
                    return await options.CloseUssdSessionAsync($"{recipient} is not a registered TechPay account.");
                }
                
                var wallet = await new GetWalletBalanceCommand
                {
                    PhoneNumber = session.Msisdn
                }.ExecuteAsync();
                
                if (decimal.Parse(amount) > wallet.Balance)
                {
                   return await options.CloseUssdSessionAsync("You have insufficient funds. Please check your wallet balance.");
                }
                
                BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                    .ProcessWalletToWalletTransfer(new WalletToWalletRequest
                    {
                        Amount = decimal.Parse(amount),
                        Sender = session.Msisdn,
                        Receiver = recipient
                    }));
                
                return await options.CloseUssdSessionAsync("Your request is being processed, you shall receive a confirmation message.");
            })
            .Case("0", o => o.NavigateTo<MainMenu>())
            .Case("p", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}

public class WalletToMobileMoneyMenu(IPhoneNumberValidator validator) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Enter the recipient's phone number (09********/07********):")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                var validatorResponse = validator.AddCountryCode(value);
                
                if (!validatorResponse.IsValid)
                {
                    return await options.CloseUssdSessionAsync("Phone number is invalid");
                }
                
                await session.SaveAsync(SessionKeys.RecipientPhoneNumber, validatorResponse.PhoneNumber);
                return await options.NavigateTo<WalletToMobileAmountMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class WalletToMobileAmountMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Enter the amount to send:")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                await session.SaveAsync(SessionKeys.Amount, value);
                return await options.NavigateTo<WalletToMobileAmountMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class WalletToMobileConfirmMenu(INasdacUssdService nasdacUssdService) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        var amount = await session.GetAsync<string>(SessionKeys.Amount);
        var recipient = await session.GetAsync<string>(SessionKeys.RecipientPhoneNumber);
        
        return await options.SetPrompt($"You are sending K{amount} to {recipient}")
            .AddOption("1. Confirm")
            .AddOption("2. Cancel\n")
            .AddOption("Press 0 for main menu or p for previous menu")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", async options =>
            {
                var recipient = await session.GetAsync<string>(SessionKeys.RecipientPhoneNumber);
                var amount = await session.GetAsync<string>(SessionKeys.Amount);
                
                var wallet = await new GetWalletBalanceCommand
                {
                    PhoneNumber = session.Msisdn
                }.ExecuteAsync();
                
                if (decimal.Parse(amount) > wallet.Balance)
                {
                    return await options.CloseUssdSessionAsync("You have insufficient funds. Please check your wallet balance.");
                }
                
                BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                    .ProcessWalletToMobileMoney(new WalletToMobileMoneyRequest
                    {
                        Amount = decimal.Parse(amount),
                        SenderPhoneNumber = session.Msisdn,
                        RecipientPhoneNumber = recipient
                    }));
                
                return await options.CloseUssdSessionAsync("Your request is being processed, you shall receive a confirmation message.");
            })
            .Case("0", o => o.NavigateTo<MainMenu>())
            .Case("p", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}

public class ManageWalletMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Select:")
            .AddOption("1. Deposit")
            .AddOption("2. Withdraw")
            .AddOption("3. View Balance")
            .AddOption("4. View Sent Vouchers")
            .AddOption("Press 0 for main menu or p for previous menu")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", o => o.NavigateTo<DepositMenu>())
            .Case("2", o => o.NavigateTo<WithdrawMenu>())
            .Case("3", o => o.NavigateTo<ViewBalanceMenu>())
            .Case("4", o => o.NavigateTo<ViewSentVouchersMenu>())
            .Case("0", o => o.NavigateTo<MainMenu>())
            .Case("p", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}

public class DepositMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Enter the amount to deposit:")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                await session.SaveAsync(SessionKeys.Amount, value);
                return await options.NavigateTo<DepositConfirmationMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class DepositConfirmationMenu(INasdacUssdService nasdacUssdService) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        var depositAmount = await session.GetAsync<string>(SessionKeys.Amount);
        
        return await options.SetPrompt($"You are about to deposit K{depositAmount} in your wallet:")
            .AddOption("1. Confirm")
            .AddOption("2. Cancel\n")
            .AddOption("Press 0 for main menu or p for previous menu")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", async options =>
            {
                var depositAmount = await session.GetAsync<string>(SessionKeys.Amount);
                
                BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                .ProcessWalletDeposit(new WalletTransactionRequest
                {
                    Amount = decimal.Parse(depositAmount),
                    Msisdn = session.Msisdn
                }));
                
                return await options.CloseUssdSessionAsync("Your request is being processed, you shall receive a prompt to enter your PIN.");
            })
            .Case("0", o => o.NavigateTo<MainMenu>())
            .Case("p", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}

public class WithdrawMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Enter the amount to withdraw:")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                await session.SaveAsync(SessionKeys.Amount, value);
                return await options.NavigateTo<WithdrawConfirmationMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class WithdrawConfirmationMenu(INasdacUssdService nasdacUssdService) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        var withdrawAmount = await session.GetAsync<string>(SessionKeys.Amount);
        
        return await options.SetPrompt($"You are about to withdraw K{withdrawAmount} from your wallet:")
            .AddOption("1. Confirm")
            .AddOption("2. Cancel\n")
            .AddOption("Press 0 for main menu or p for previous menu")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", async options =>
            {
                var withdrawAmount = await session.GetAsync<string>(SessionKeys.Amount);
                
                var result = await new GetWalletBalanceCommand
                {
                    PhoneNumber = session.Msisdn
                }.ExecuteAsync();
                
                if (decimal.Parse(withdrawAmount) > result.Balance)
                {
                    return await options.CloseUssdSessionAsync("You have insufficient funds. Please check your wallet balance.");
                }
                
                BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                    .ProcessWalletWithdraw(new WalletTransactionRequest
                    {
                        Amount = decimal.Parse(withdrawAmount),
                        Msisdn = session.Msisdn
                    }));
                
                return await options.CloseUssdSessionAsync("Your request is being processed, you shall receive a confirmation message.");
            })
            .Case("0", o => o.NavigateTo<MainMenu>())
            .Case("p", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}

public class ViewBalanceMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        var result = await new GetWalletBalanceCommand
        {
            PhoneNumber = session.Msisdn
        }.ExecuteAsync();
        
        return await options.SetPrompt($"You account balance is K{result.Balance}\n")
            .AddOption("Press 0 for main menu or p for previous menu")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("0", o => o.NavigateTo<MainMenu>())
            .Case("p", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}

public class ViewSentVouchersMenu(NasdacDatabase database) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        var member = await database.Members.FirstOrDefaultAsync(x => x.PhoneNumber == session.Msisdn);
        
        if (member == null)
        {
            return await options.SetPrompt("Member with the given number not found.")
                .BuildAsync();
        }
        
        var voucherTransaction = await database.VoucherTransactions
            .Include(x => x.WalletTransaction)
            .Where(x => x.VoucherStatus == VoucherStatus.Open)
            .Where(x => x.MemberId == member.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        
        var text = voucherTransaction.Count != 0
            ? "Select:"
            : "No eWallets available at the moment.";
        
        var display = voucherTransaction
            .Select((x, i) => $"{i + 1}. {x.WalletTransaction.Amount} - {x.WalletTransaction.BilledAccount} - {x.Code}")
            .ToList();
        
        return await options
            .SetPrompt(text)
            .AddOption(string.Join("\n", display))
            .AddOption("Press p for previous menu or 0 to cancel")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("0", o => o.NavigateTo<MainMenu>())
            .Case("p", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using TechPay."));
    }
}
