using UssdDevelopmentCore.Services;
using UssdDevelopmentCore.Utilities;

namespace UssdDevelopmentCore.Extensions;

public static class ApplicationServicesExtension
{
    public static void AddApplicationServices(this IServiceCollection service)
    {
        service.AddScoped<IPhoneNumberValidator, PhoneNumberValidator>();
        service.AddScoped<IPaymentService, PaymentService>();
        service.AddScoped<INasdacUssdService, NasdacUssdService>();
        service.AddScoped<IGeneratorService, GeneratorService>();
    }
}