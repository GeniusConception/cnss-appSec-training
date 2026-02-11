using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Lab 9 : Config Serilog avec enrichissement ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext() // Indispensable pour LogContext.PushProperty
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// --- Lab 9 : Config Authentification (Simulation) ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "UneCleSecreteSuperLonguePourLeLab9!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// --- Lab 9 : Health Checks ---
builder.Services.AddHealthChecks();

var app = builder.Build();

// --- Middleware : En-têtes de Sécurité (DAST/OWASP ZAP) ---
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    await next();
});

// --- Middleware : Enrichissement des Logs (Observabilité) ---
app.Use(async (context, next) =>
{
    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
    using (LogContext.PushProperty("UserId", userId))
    {
        await next();
    }
});

app.UseAuthentication();
app.UseAuthorization();

// --- Lab 9 : Health Check Endpoint ---
app.MapHealthChecks("/health");

// --- Lab 9 : Endpoints protégés ---
app.MapGet("/api/dossier/{id}", (int id) => 
{
    return Results.Ok(new { Id = id, Title = $"Dossier Confidentiel n°{id}", Owner = "CNSS" });
})
.RequireAuthorization();

app.MapGet("/", () => "API Observabilité (Lab 9) active.");

app.Run();

// Requis pour les tests d'intégration avec WebApplicationFactory
public partial class Program { }
