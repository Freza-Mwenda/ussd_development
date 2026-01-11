using FastEndpoints;
using Flurl.Http;
using UssdDevelopmentCore.Entities;

namespace UssdDevelopmentCore.Commands;

public class GetClientKycCommand : ICommand<KycResponse>
{
    public required string PhoneNumber { get; set; }
}

public class GetClientKycCommandHandler : ICommandHandler<GetClientKycCommand, KycResponse>
{
    public Task<KycResponse> ExecuteAsync(GetClientKycCommand command, CancellationToken ct)
    {
        string GetKycUrl(string phoneNumber)
        {
            var index = phoneNumber[4].ToString();
        
            return index switch
            {
                "7" => "https://patumba-airtel.hobbiton.app/kyc",
                "6" => "https://patumba-mtn.hobbiton.dev/kyc",
                "5" => "https://zamtel-kyc.hobbiton.io/kyc",
                _ => throw new Exception("Invalid phone number")
            };
        }
        
        return $"{GetKycUrl(command.PhoneNumber)}/{command.PhoneNumber}"
            .GetJsonAsync<KycResponse>(cancellationToken: ct);
    }
}