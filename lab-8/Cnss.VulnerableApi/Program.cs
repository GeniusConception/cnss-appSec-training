using Serilog;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// --- Lab 8 : Simulation d'un "Leaked Secret" ---
// ATTENTION : Ne jamais faire cela en production !
const string GOOGLE_API_KEY = "AIzaSyA1234567890-fake-google-key";
const string DB_CONNECTION = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword123!;";

// --- Configuration Serilog ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.MapGet("/", () => 
{
    Log.Information("Accès à la racine avec la clé : {Key}", GOOGLE_API_KEY);
    
    var data = new { Message = "Lab 8 : Quality Gate", Status = "Vulnerable" };
    // Utilisation d'une version ancienne de Newtonsoft.Json (v11.0.1)
    string json = JsonConvert.SerializeObject(data);
    
    return Results.Content($@"
        <html>
            <body style='font-family: sans-serif; text-align: center; padding-top: 50px;'>
                <h1 style='color: #d93025;'>CNSS AppSec Training - Lab 8</h1>
                <p>Cette application contient des vulnérabilités critiques détectables par pipeline.</p>
                <code style='background: #eee; padding: 10px;'>{json}</code>
            </body>
        </html>", "text/html");
});

// 1. VULNÉRABILITÉ : Path Traversal
app.MapGet("/api/files", (string fileName) => 
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileName);
    if (!File.Exists(path)) return Results.NotFound();
    
    var content = File.ReadAllText(path);
    return Results.Text(content);
});

// 2. VULNÉRABILITÉ : Cross-Site Scripting (XSS)
app.MapGet("/api/hello", (string name) => 
{
    // Injection directe de l'entrée utilisateur dans du HTML (Reflected XSS)
    return Results.Content($"<h1>Hello, {name}</h1>", "text/html");
});

// 3. VULNÉRABILITÉ : Command Injection
app.MapGet("/api/ping", (string host) => 
{
    // Utilisation directe de l'entrée utilisateur dans une commande système
    var process = new System.Diagnostics.Process();
    process.StartInfo.FileName = "ping.exe";
    process.StartInfo.Arguments = host; // Ex: "google.com && whoami"
    process.StartInfo.RedirectStandardOutput = true;
    process.Start();
    
    string result = process.StandardOutput.ReadToEnd();
    return Results.Text(result);
});

// 4. VULNÉRABILITÉ : Insecure Deserialization
app.MapPost("/api/config", (string json) => 
{
    // Utilisation de TypeNameHandling.All est extrêmement dangereux (RCE possible)
    var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
    var obj = JsonConvert.DeserializeObject(json, settings);
    return Results.Ok(new { Message = "Configuration mise à jour" });
});

app.Run();
