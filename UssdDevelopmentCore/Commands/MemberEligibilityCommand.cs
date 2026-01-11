using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;

namespace UssdDevelopmentCore.Commands;

public class MemberEligibilityCommand : ICommand<bool>
{
    public required string PhoneNumber { get; set; }
}

public class MemberEligibilityCommandHandler(IServiceScopeFactory factory) : ICommandHandler<MemberEligibilityCommand, bool>
{
    public async Task<bool> ExecuteAsync(MemberEligibilityCommand command, CancellationToken ct)
    {
        var database = factory.CreateScope().ServiceProvider.GetRequiredService<NasdacDatabase>();
        
        var isNasdacMember = await database.Members.AnyAsync(x => x.PhoneNumber == command.PhoneNumber, cancellationToken: ct);

        return isNasdacMember;
    }
}