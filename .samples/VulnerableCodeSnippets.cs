using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace Cnss.VulnerableApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnsafeController : ControllerBase
{
    private readonly string _connectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword123;"; // VULNÉRABILITÉ : Secret en dur

    [HttpGet("search")]
    public IActionResult SearchUser(string name)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            
            // VULNÉRABILITÉ : Injection SQL (Concaténation de chaîne)
            // Un attaquant peut saisir : " ' OR 1=1 -- "
            var query = "SELECT * FROM Users WHERE Username = '" + name + "'";
            
            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    // Lecture des résultats...
                    return Ok("Recherche effectuée");
                }
            }
        }
    }

    [HttpGet("redirect")]
    public IActionResult RedirectUser(string url)
    {
        // VULNÉRABILITÉ : Open Redirect (Redirection non validée)
        // Un attaquant peut rediriger l'assuré vers un site de phishing : ?url=https://malveillant.com
        return Redirect(url);
    }

    [HttpPost("upload")]
    public IActionResult UploadFile(IFormFile file)
    {
        // VULNÉRABILITÉ : Path Traversal & Absence de vérification du type de fichier
        // Un attaquant peut uploader un fichier .exe ou tenter d'écraser un fichier système
        var filePath = Path.Combine("C:\\uploads", file.FileName);
        
        using (var stream = System.IO.File.Create(filePath))
        {
            file.CopyTo(stream);
        }

        return Ok("Fichier sauvegardé");
    }
}