using System.Linq.Expressions;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;

namespace UssdDevelopmentCore.Extensions;

public static class BackgroundJobExtension
{
    public static void AddBackGroundJobs(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<HangfireConfig>(config.GetSection(HangfireConfig.SectionName));
        var hangfireConfig = config.GetSection(HangfireConfig.SectionName).Get<HangfireConfig>();

        if (hangfireConfig is null)
        {
            throw new Exception("Missing Hangfire Configurations in appsettings.json");
        }

        services.AddHangfire(globalConfiguration => globalConfiguration
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage()
        );

        services.AddHangfireServer((_, options) =>
        {
            options.ServerName = hangfireConfig.ServerName;
            options.Queues = hangfireConfig.Queues;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(60);
            options.WorkerCount = Environment.ProcessorCount * 30;
        });
    }

    public static void UseBackgroundService(this IApplicationBuilder app)
    {
        try
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static void TryEnqueue(Expression<Func<Task>> methodCall)
    {
        try
        {
            BackgroundJob.Enqueue(methodCall);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

public class HangfireConfig
{
    public required string ServerName { get; set; }
    public required string[] Queues { get; set; } = Array.Empty<string>();
    public const string SectionName = "Hangfire";
}

public class HangfireAuthorizationFilter: IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity is { IsAuthenticated: false };
    }
}