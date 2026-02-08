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

app.UseExceptionHandler();

// --- Lab 4 : Headers de Sécurité (Native & Custom) ---
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

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
app.UseRateLimiter(); // Middleware nécessaire pour appliquer les politiques
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- Lab 4 : Endpoint de Login simulé avec Rate Limiting ---
app.MapPost("/api/login", () => 
{
    return Results.Ok(new { message = "Tentative de connexion reçue." });
})
.RequireRateLimiting("LoginPolicy")
.WithName("Login");

// --- Lab 3 : Endpoint de Génération de Token ---
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
        Expires = DateTime.UtcNow.AddMinutes(15),
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
    var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? user.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden); 
    }

    return Results.Ok(new 
    { 
        DossierId = id, 
        Assure = "Jean Dupont", 
        Status = "Authentifié et Vérifié",
        Message = "Accès autorisé."
    });
})
.RequireAuthorization("ReadDossierPolicy")
.WithName("GetDossier");

app.MapGet("/", () => "API Lab 4 (Hardening) active. Login protégé par Rate Limiting.");

app.Run();
