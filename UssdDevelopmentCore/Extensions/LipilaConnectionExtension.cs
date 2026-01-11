namespace UssdDevelopmentCore.Extensions;

public class LipilaConnection
{
    public const string SectionName = "Lipila";
    public required string ApiKey { get; set; }
    public required string BaseUrl { get; set; }
}

public static class LipilaConnectionExtension
{
    public static void AddLipilaConnection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LipilaConnection>()
            .Bind(configuration.GetSection(LipilaConnection.SectionName))
            .ValidateOnStart();
    }
}