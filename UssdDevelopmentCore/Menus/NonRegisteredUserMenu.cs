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

public class NonRegisteredUserMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Welcome to NASDAC Global Money:")
            .AddOption("1. Register")
            .AddOption("2. Receive Money")
            .AddOption("3. Cancel")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", o => o.NavigateTo<RegisterMenu>())
            .Case("2", o => o.NavigateTo<ReceiveMoneyMenu>())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using NASDAC Global Money."));
    }
}

public class RegisterMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Select:")
            .AddOption("1. Accept Ts and Cs")
            .AddOption("Press 0 for previous menu or 2 to cancel")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", o => o.NavigateTo<WalletPinMenu>())
            .Case("0", o => o.NavigateBackAsync())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using NASDAC Global Money."));
    }
}

public class WalletPinMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Create a NASDAC wallet PIN:")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                await session.SaveAsync(SessionKeys.NewPin, value);
                return await options.NavigateTo<ConfirmWalletPinMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class ConfirmWalletPinMenu: IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Confirm NASDAC wallet PIN:")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                var walletPin = await session.GetAsync<string>(SessionKeys.NewPin);

                if (walletPin != value)
                {
                    return await options.CloseUssdSessionAsync("Confirmation wallet PIN does not match new PIN.");
                }
                
                await session.SaveAsync(SessionKeys.NewPin, walletPin);
                return await options.NavigateTo<AcceptTermsAndConditionsMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class AcceptTermsAndConditionsMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("To proceed with NASDAC registration:")
            .AddOption("1. Confirm")
            .AddOption("2. Cancel")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", async options =>
            {
                var walletPin = await session.GetAsync<string>(SessionKeys.NewPin);
                var kyc = await new GetClientKycCommand { PhoneNumber = session.Msisdn }.ExecuteAsync();
                
                await new CreateClientCommand
                {
                    PhoneNumber = session.Msisdn,
                    FirstName = kyc.FirstName,
                    LastName = kyc.LastName,
                    WalletPin = walletPin
                }.ExecuteAsync();
                
                return await options.NavigateTo<MainMenu>();
            })
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using NASDAC Global Money."));
    }
}

public class ReceiveMoneyMenu : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Select:")
            .AddOption("1. View Vouchers")
            .AddOption("2. Cancel")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Case("1", o => o.NavigateTo<ViewVouchersMenu>())
            .Default((o, ex) => o.CloseUssdSessionAsync("Thank you for using NASDAC Global Money."));
    }
}

public class ViewVouchersMenu(NasdacDatabase database) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        var voucherTransaction = await database.VoucherTransactions
            .Include(x => x.WalletTransaction)
            .Where(x => x.VoucherStatus == VoucherStatus.Open)
            .Where(x => x.Recipient == session.Msisdn)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        
        var text = voucherTransaction.Count != 0
            ? "Select:"
            : "No eWallets available at the moment.";
        
        await session.SaveAsync(SessionKeys.Vouchers, voucherTransaction);
        
        var display = voucherTransaction
            .Select((x, i) => $"{i + 1}. {x.WalletTransaction.Amount} - {x.WalletTransaction.BilledAccount}")
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
            .Process(async (options, value) =>
            {
                switch (value)
                {
                    case "p":
                        return await options.NavigateBackAsync();
                    case "0":
                        return await options.CloseUssdSessionAsync("Thank you for using NASDAC Global Money.");
                }

                var vouchers = await session.GetAsync<List<VoucherTransaction>>(SessionKeys.Vouchers);
                var option = int.Parse(value);
                var selectedVouchers = vouchers[option - 1];
                await session.SaveAsync(SessionKeys.SelectedVoucher, selectedVouchers);
                
                return await options.NavigateTo<VoucherPinMenu>();
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}

public class VoucherPinMenu(INasdacUssdService nasdacUssdService) : IUssdMenu
{
    public async Task<string> DisplayAsync(IDisplayOptions options, IUssdSession session)
    {
        return await options.SetPrompt("Enter Voucher PIN to proceed:")
            .BuildAsync();
    }

    public async Task<ProcessedInput> ProcessInputAsync(IUssdInput input, IUssdSession session)
    {
        return await input.Parse()
            .Process(async (options, value) =>
            {
                var selectedVoucher = await session.GetAsync<VoucherTransaction>(SessionKeys.SelectedVoucher);
                
                var voucherPin = value;
                
                var result = await new CheckVoucherCodeCommand
                {
                    VoucherId = selectedVoucher.Id,
                    Code = voucherPin
                }.ExecuteAsync();
                
                if (result)
                {
                    BackgroundJobExtension.TryEnqueue(() => nasdacUssdService
                        .ProcessReceiveMoney(new ReceiveMoneyRequest
                        {
                            VoucherId = selectedVoucher.Id,
                            Code = voucherPin
                        }));
                        
                    return await options.CloseUssdSessionAsync("Your request is being processed, you shall receive a confirmation message.");
                }

                return await options.CloseUssdSessionAsync("The entered Voucher PIN is incorrect.");
            })
            .Default((options, _) => options.CloseUssdSessionAsync("Invalid Operation."));
    }
}