using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Simulation d'une base de clés partenaires
var partnerKeys = new Dictionary<string, string> {
    { "Partner_CNSS_001", "MaCléSecrèteSuperGigaSecurisée" }
};

app.MapPost("/api/partner/transfert", async (HttpContext context) =>
{
    // 1. Extraction des en-têtes de sécurité
    if (!context.Request.Headers.TryGetValue("X-Partner-Id", out var partnerId) ||
        !context.Request.Headers.TryGetValue("X-Signature", out var receivedSignature) ||
        !context.Request.Headers.TryGetValue("X-Timestamp", out var timestampStr))
    {
        return Results.Unauthorized();
    }

    // 2. Protection contre le rejeu (5 minutes max)
    var requestTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestampStr!));
    if (DateTimeOffset.UtcNow.Subtract(requestTime).TotalMinutes > 5)
    {
        return Results.Problem("Requête expirée (Replay Attack?)", statusCode: 401);
    }

    // 3. Vérification de la signature
    if (!partnerKeys.TryGetValue(partnerId!, out var secret)) return Results.Unauthorized();

    // Lecture du corps de la requête pour le calcul
    context.Request.EnableBuffering();
    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0;

    // Calcul du HMAC (On signe : Timestamp + URL + Body)
    string payloadToSign = $"{timestampStr}{context.Request.Path}{body}";
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadToSign));
    var computedSignature = Convert.ToBase64String(computedHash);

    if (computedSignature != receivedSignature)
    {
        return Results.Problem("Signature invalide - Le message a été altéré ou la clé est fausse.", statusCode: 401);
    }

    return Results.Ok(new { Status = "Succès", Message = "Authenticité et intégrité confirmées." });
});

app.Run();

/* POUR TESTER (Console App C#) :
-----------------------------
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
var body = "{\"montant\": 1000}";
var payload = timestamp + "/api/partner/transfert" + body;
var signature = Convert.ToBase64String(new HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(Encoding.UTF8.GetBytes(payload)));

client.DefaultRequestHeaders.Add("X-Partner-Id", "Partner_CNSS_001");
client.DefaultRequestHeaders.Add("X-Signature", signature);
client.DefaultRequestHeaders.Add("X-Timestamp", timestamp);
*/