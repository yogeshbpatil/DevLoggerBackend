using DevLoggerBackend.Api.Extensions;
using DevLoggerBackend.Application;
using DevLoggerBackend.Infrastructure;
using Serilog;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}
else
{
    builder.WebHost.UseUrls("http://localhost:5000");
}

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/devlogger-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddHealthChecks();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var swaggerUrl = "http://localhost:5000/swagger";
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {swaggerUrl}") { CreateNoWindow = true });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", swaggerUrl);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", swaggerUrl);
            }
        }
        catch
        {
        }
    });
}

var applyMigrationsOnStartup =
    app.Configuration.GetValue<bool?>("ApplyMigrationsOnStartup") ?? app.Environment.IsDevelopment();

if (applyMigrationsOnStartup)
{
    await app.ApplyDatabaseMigrationsAsync();
}

app.UseApiPipeline();
app.Run();
