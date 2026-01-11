using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using UssdDevelopmentCore.Common;
using UssdDevelopmentCore.Extensions;
using UssdStateMachine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Default", policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.AllowAnyOrigin();
    });
});

builder.Services.AddFastEndpoints()
    .SwaggerDocument()
    .ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<Provider>(builder.Configuration.GetSection("Provider"));
builder.Services.Configure<SmsService>(builder.Configuration.GetSection("SmsService"));
builder.Services.AddControllers();
builder.Services.AddUssdMenus();
builder.Services.AddApplicationServices();
builder.Services.AddPostgresDatabase(builder.Configuration);
builder.Services.AddLipilaConnection(builder.Configuration);
builder.Services.AddBackGroundJobs(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseRouting();
app.UseCors(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseBackgroundService();
app.MapControllers();

app.UseFastEndpoints(c =>
    {
        c.Versioning.Prefix = "v1";
        c.Endpoints.RoutePrefix = "api/v1";
    
    }).UseSwaggerGen()
    .UseDefaultExceptionHandler();

app.Run();