using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// --- Lab 6 : Étape 2 : Nettoyage des signatures du serveur ---
// On désactive le header "Server: Kestrel" pour ne pas divulguer d'infos techniques
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

// --- Lab 1 & 6 : Logging avec Serilog ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// --- Lab 6 : Étape 1 : Configuration des Forwarded Headers ---
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    
    // Pour la démo, on autorise les proxies locaux (127.0.0.1)
    // Sur un VPS réel, on pourrait restreindre à l'IP du proxy Caddy
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();


// Activation du middleware pour lire les en-têtes X-Forwarded-*
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// --- Lab 6 : Étape 3 : Route Home avec IP Réelle ---
app.MapGet("/", (HttpContext context) => 
{
    var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "Inconnue";
    
    // Log de l'IP réelle via Serilog
    Log.Information("Requête reçue de l'IP : {RemoteIp}", remoteIp);

    return Results.Content($@"
        <html>
            <body style='font-family: sans-serif; display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100vh; background-color: #f0f2f5;'>
                <div style='background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); text-align: center;'>
                    <h1 style='color: #1a73e8;'>CNSS AppSec Training - Lab 6</h1>
                    <p style='font-size: 1.2rem;'>Hello World from <strong>{remoteIp}</strong></p>
                    <p style='color: #5f6368;'>Server signature is hidden.</p>
                </div>
            </body>
        </html>", "text/html");
});

app.Run();
