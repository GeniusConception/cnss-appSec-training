using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// --- Lab 1 : Logging Structuré ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// --- Lab 1 : ProblemDetails ---
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- Lab 2 : Exercice 4 - Protection BOLA ---

// Endpoint simulant la consultation d'un dossier assuré
app.MapGet("/dossier/{id}", ([FromRoute] string id, [FromHeader(Name = "X-User-Id")] string currentUserId) => 
{
    // Simulation d'un contrôle de propriété (Ownership Check)
    // IMPORTANT : On ne fait jamais confiance à l'ID fourni dans l'URL.
    // On doit toujours le comparer à l'ID issu de l'identité sécurisée (JWT, Session).
    
    if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
    {
        // On utilise StatusCode(403) car Results.Forbid() nécessite l'authentification configurée
        return Results.StatusCode(StatusCodes.Status403Forbidden); 
    }

    return Results.Ok(new 
    { 
        DossierId = id, 
        Assure = "Jean Dupont", 
        Adresse = "123 Rue de la CNSS, Casablanca",
        Message = "Accès autorisé : Vous êtes bien le propriétaire de ce dossier."
    });
})
.WithName("GetDossier");


app.MapGet("/error", () => 
{
    throw new Exception("Ceci est une erreur interne !");
});

app.MapGet("/", () => "API Lab 2 (BOLA Prevention) active. Testez /dossier/{id} avec le header X-User-Id.");

app.Run();
