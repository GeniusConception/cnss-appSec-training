using Serilog;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration Serilog ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// --- Lab 7 : Middleware de Vérification HMAC ---
app.Use(async (context, next) =>
{
    // On ne vérifie que les requêtes vers l'API de transactions
    if (!context.Request.Path.StartsWithSegments("/api/transactions"))
    {
        await next();
        return;
    }

    // 1. Extraction des headers obligatoires
    if (!context.Request.Headers.TryGetValue("X-Signature", out var receivedSignature) ||
        !context.Request.Headers.TryGetValue("X-Timestamp", out var receivedTimestampStr))
    {
        Log.Warning("Hmac Verification Failed: Headers manquants.");
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Requête non authentifiée (Headers manquants).");
        return;
    }

    // 2. Vérification du Replay Attack (± 5 minutes)
    if (!long.TryParse(receivedTimestampStr, out var timestamp))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Timestamp invalide.");
        return;
    }

    var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
    var now = DateTimeOffset.UtcNow;
    var timeDifference = now - requestTime;

    if (Math.Abs(timeDifference.TotalMinutes) > 5)
    {
        Log.Warning("Hmac Verification Failed: Replay attack détecté (Décalage: {Delay} min).", Math.Round(timeDifference.TotalMinutes, 2));
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Requête expirée (Replay Attack Protection).");
        return;
    }

    // 3. Récupération de la clé partagée
    // PartnerSecrets:BanqueABC est chargé via Secrets .NET ou Variable d'env (PARTNERSECRETS__BANQUEABC)
    var sharedSecret = app.Configuration["PartnerSecrets:BanqueABC"];
    if (string.IsNullOrEmpty(sharedSecret))
    {
        Log.Error("Configuration Error: Clé secrète BanqueABC manquante.");
        context.Response.StatusCode = 500;
        return;
    }

    // 4. Lecture du corps pour la signature
    context.Request.EnableBuffering();
    string body;
    using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
    {
        body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
    }

    // 5. Calcul de la signature locale (Format: timestamp:body)
    var dataToHash = $"{receivedTimestampStr}:{body}";
    var keyBytes = Encoding.UTF8.GetBytes(sharedSecret);
    var dataBytes = Encoding.UTF8.GetBytes(dataToHash);

    using var hmac = new HMACSHA256(keyBytes);
    var hashBytes = hmac.ComputeHash(dataBytes);
    var computedSignature = Convert.ToBase64String(hashBytes);

    // 6. Comparaison
    if (computedSignature != receivedSignature)
    {
        Log.Warning("Hmac Verification Failed: Signature invalide.");
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Signature invalide. Intégrité des données compromise.");
        return;
    }

    Log.Information("Hmac Verification Success: Signature valide pour BanqueABC.");
    await next();
});

app.MapPost("/api/transactions", async (HttpContext context) =>
{
    return Results.Ok(new { status = "Success", message = "Transaction reçue et authentifiée." });
});

app.MapGet("/", () => "API Hmac (Lab 7) active.");

app.Run();
