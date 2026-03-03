using DevLoggerBackend.Api.Extensions;
using DevLoggerBackend.Application;
using DevLoggerBackend.Infrastructure;
using Serilog;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey!)
        )
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
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

var enableSwagger = app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("EnableSwaggerInProduction");

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();

    if (app.Environment.IsDevelopment())
    {
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
}

var applyMigrationsOnStartup =
    app.Configuration.GetValue<bool?>("ApplyMigrationsOnStartup") ?? app.Environment.IsDevelopment();

if (applyMigrationsOnStartup)
{
    await app.ApplyDatabaseMigrationsAsync();
}

app.UseApiPipeline();
app.Run();
