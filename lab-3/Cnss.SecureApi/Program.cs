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

app.UseExceptionHandler();

app.UseCors("CnssPortalPolicy");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- Lab 3 : Endpoint de Génération de Token (Pour la démo) ---
app.MapGet("/api/auth/token", (string userId, string? scope = null) => 
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId),
        new Claim(JwtRegisteredClaimNames.UniqueName, userId),
        new Claim("scope", scope ?? "dossier.read"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = "cnss-auth-server",
        Audience = "cnss-api",
        SigningCredentials = credentials
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    
    return Results.Ok(new { access_token = tokenHandler.WriteToken(token) });
})
.WithName("GenerateToken");

// --- Lab 3 : Endpoint Sécurisé avec Protection BOLA ---
app.MapGet("/api/dossier/{id}", ([FromRoute] string id, ClaimsPrincipal user) => 
{
    // Extraction simplifiée du sub du token
    var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? user.FindFirst("sub")?.Value;

    // Contrôle BOLA (Propriété de l'objet)
    if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden); 
    }

    return Results.Ok(new 
    { 
        DossierId = id, 
        Assure = "Jean Dupont", 
        Status = "Authentifié et Vérifié",
        Message = "Accès autorisé : JWT validé et propriétaire vérifié."
    });
})
.RequireAuthorization("ReadDossierPolicy")
.WithName("GetDossier");

app.MapGet("/", () => "API Lab 3 (JWT Pédagogique) active. Utilisez /api/auth/token?userId=123 pour obtenir un jeton.");

app.Run();
