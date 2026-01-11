using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;

namespace UssdDevelopmentCore.Commands;

public class CheckVoucherCodeCommand : ICommand<bool>
{
    public required int VoucherId { get; set; }
    public required string Code { get; set; }
}

public class CheckVoucherCodeCommandHandler(IServiceScopeFactory factory)
    : ICommandHandler<CheckVoucherCodeCommand, bool>
{
    public async Task<bool> ExecuteAsync(CheckVoucherCodeCommand command, CancellationToken ct)
    {
        var database = factory.CreateScope().ServiceProvider.GetRequiredService<NasdacDatabase>();
        
        var voucherTransaction = await database.VoucherTransactions
            .FirstOrDefaultAsync(x => x.Id == command.VoucherId, cancellationToken: ct);

        if (voucherTransaction == null)
        {
            throw new Exception("Voucher transaction not found");
        }
        
        return voucherTransaction.Code == command.Code;
    }
}