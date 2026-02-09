using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// --- Lab 3 : Configuration Clé de Signature (Pédagogique) ---
const string JWT_KEY = "CléSecrèteSuperLonguePourLeLabSécuritéCNSS2026!";
var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWT_KEY));

// --- Lab 1 : Logging Structuré ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// --- Lab 5 : Base de données SQLite ---
// La chaîne de connexion est lue depuis les User Secrets (Lab 5 Étape 3)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// --- Lab 5 : Validation Robuste ---
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// --- Lab 5 : Protection des Données (Chiffrement) ---
builder.Services.AddDataProtection();

// --- Lab 4 : Configuration du Rate Limiting ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "LoginPolicy", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromSeconds(10);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// --- Lab 3 : Configuration CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("CnssPortalPolicy", policy =>
    {
        policy.WithOrigins("https://portal.cnss.cd")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// --- Lab 3 : Configuration Authentification JWT ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "cnss-auth-server",
            ValidateAudience = true,
            ValidAudience = "cnss-api",
            ValidateLifetime = true,
            IssuerSigningKey = securityKey
        };
    });

// --- Lab 3 : Configuration Autorisation & Scopes ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadDossierPolicy", policy => 
        policy.RequireAuthenticatedUser()
              .RequireClaim("scope", "dossier.read"));
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddOpenApi();

var app = builder.Build();

// --- Initialisation de la base pour le lab ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseExceptionHandler();

// --- Lab 4 : Headers de Sécurité (Native & Custom) ---
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; script-src 'self'");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});

app.UseHttpsRedirection();
app.UseCors("CnssPortalPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- Lab 5 : Inscription avec Validation ---
app.MapPost("/api/assure/register", async ([FromBody] RegisterAssureRequest request, IValidator<RegisterAssureRequest> validator, IDataProtectionProvider dataProtection, AppDbContext db) => 
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    // Chiffrement de l'email avant affichage/stockage (Lab 5 Étape 4)
    var protector = dataProtection.CreateProtector("EmailProtector");
    var encryptedEmail = protector.Protect(request.Email);

    // PERSISTANCE RÉELLE DANS LA BASE (Utilisée pour le Lab de recherche)
    var nouvelAssure = new Assure 
    { 
        Nom = request.Nom, 
        Ssn = request.Ssn 
    };
    db.Assures.Add(nouvelAssure);
    await db.SaveChangesAsync();

    Log.Information("Nouvel assuré enregistré ! Email chiffré : {encryptedEmail}", encryptedEmail);

    return Results.Created($"/api/dossier/{request.Ssn}", new 
    { 
        Message = "Assuré enregistré avec succès dans la base SQLite.",
        EncryptedEmailExample = encryptedEmail
    });
});

// --- Lab 5 : Recherche sécurisée vs Vulnérable ---
app.MapGet("/api/assure/search", async (string name, AppDbContext db) => 
{
    // --- MAUVAISE PRATIQUE (Simulation de vulnérabilité) ---
    // ATTENTION : La concaténation de chaînes permet une injection SQL.
    var query = $"SELECT * FROM Assures WHERE Nom = '{name}'"; 
    var result = await db.Assures.FromSqlRaw(query).ToListAsync();

    // --- BONNE PRATIQUE (Requête paramétrée avec LINQ) ---
    // LINQ génère automatiquement des paramètres (ex: @p0), empêchant l'injection.
    // var result = await db.Assures
    //     .Where(a => a.Nom == name)
    //     .ToListAsync();

    return Results.Ok(result);
});

// --- Lab 4 : Endpoint de Login simulé avec Rate Limiting ---
app.MapPost("/api/login", () => Results.Ok(new { message = "Tentative de connexion reçue." }))
.RequireRateLimiting("LoginPolicy");

// --- Lab 3 : Endpoint de Génération de Token ---
app.MapGet("/api/auth/token", (string userId, string? scope = null) => 
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId),
        new Claim("scope", scope ?? "dossier.read"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(15),
        Issuer = "cnss-auth-server",
        Audience = "cnss-api",
        SigningCredentials = credentials
    };
    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { access_token = tokenHandler.WriteToken(token) });
});

// --- Lab 3 : Endpoint Sécurisé avec Protection BOLA ---
app.MapGet("/api/dossier/{id}", ([FromRoute] string id, ClaimsPrincipal user) => 
{
    var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(currentUserId) || currentUserId != id) return Results.StatusCode(403); 
    return Results.Ok(new { DossierId = id, Assure = "Jean Dupont", Status = "Vérifié" });
})
.RequireAuthorization("ReadDossierPolicy");

app.MapGet("/", () => "API Lab 5 (Validation & Secrets) active.");

app.Run();

// --- Modèles et Classes pour le Lab 5 ---

public class RegisterAssureRequest
{
    public string Nom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Ssn { get; set; } = string.Empty; // Numéro de Sécurité Sociale
}

public class RegisterAssureRequestValidator : AbstractValidator<RegisterAssureRequest>
{
    public RegisterAssureRequestValidator()
    {
        RuleFor(x => x.Nom).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        
        // Format SSN : 1-XX-XX-XX-XXX-XX (Simplifié)
        RuleFor(x => x.Ssn)
            .NotEmpty()
            .Matches(@"^[1-2]\d{12}$")
            .WithMessage("Le SSN doit commencer par 1 ou 2 et faire 13 chiffres.");
    }
}

public class Assure
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Ssn { get; set; } = string.Empty;
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Assure> Assures => Set<Assure>();
}
