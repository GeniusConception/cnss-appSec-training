using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// --- Étape 4 : Logging Structuré avec Serilog ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    // On ajoute le TraceId via l'enrichissement par défaut ou personnalisé si besoin
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// --- Étape 3 : Configuration de ProblemDetails (RFC 7807) ---
// Empêche la fuite d'informations techniques (Information Disclosure)
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // On peut ajouter des informations de corrélation
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        
        // En production, on s'assure qu'aucune stack trace ne fuite
        // (Par défaut AddProblemDetails gère déjà cela intelligemment)
    };
});

builder.Services.AddOpenApi();

var app = builder.Build();

// --- Étape 2 : Pipeline de sécurité ---
// Le middleware d'exception doit être l'un des premiers
app.UseExceptionHandler(); // Utilise automatiquement ProblemDetails si configuré

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Endpoint de test pour générer une erreur
app.MapGet("/error", () => 
{
    throw new Exception("Ceci est une erreur interne qui ne doit pas exposer sa stack trace !");
});

app.MapGet("/", () => "API Sécurisée active. Testez /error pour voir la gestion des erreurs.");

app.Run();
