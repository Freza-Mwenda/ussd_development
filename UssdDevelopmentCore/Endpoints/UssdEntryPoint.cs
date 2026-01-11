using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;
using UssdDevelopmentCore.Menus;
using UssdStateMachine;
using UssdStateMachine.Services;

namespace UssdDevelopmentCore.Endpoints;

public class UssdEntryPoint(IUssdService service, NasdacDatabase database) : Endpoint<UssdRequest>
{
    public override void Configure()
    {
        Get("ussd");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UssdRequest req, CancellationToken ct)
    {
        var member = await database.Members.FirstOrDefaultAsync(x => x.PhoneNumber == req.Msisdn, cancellationToken: ct);

        if (member == null)
        {
            await service.ProcessRequestAsync<NonRegisteredUserMenu>(req);
        }
        else
        {
            await service.ProcessRequestAsync<MainMenu>(req); 
        }
    }
}