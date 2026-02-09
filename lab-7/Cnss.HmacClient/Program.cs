using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RestSharp;

// --- CNSS HMAC Client (Lab 7) ---
// Ce client interactif permet de simuler l'envoi de transactions signées.

const string SHARED_SECRET = "CléSecrètePartagéeTrèsLongue";
const string SERVER_URL = "http://localhost:5007";

Console.WriteLine("=== CNSS HMAC Client Interactif ===");

while (true)
{
    Console.WriteLine("\n[NOUVELLE TRANSACTION]");
    Console.Write("Montant (ex: 1000) : ");
    var amount = Console.ReadLine();
    
    Console.Write("Destinataire (ex: Banque de France) : ");
    var recipient = Console.ReadLine();

    var payload = new
    {
        Id = Guid.NewGuid().ToString(),
        Amount = amount,
        Recipient = recipient,
        Currency = "USD"
    };

    var jsonBody = JsonConvert.SerializeObject(payload);
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    // --- Calcul HMAC-SHA256 ---
    var dataToSign = $"{timestamp}:{jsonBody}";
    var keyBytes = Encoding.UTF8.GetBytes(SHARED_SECRET);
    var dataBytes = Encoding.UTF8.GetBytes(dataToSign);

    string signature;
    using (var hmac = new HMACSHA256(keyBytes))
    {
        var hashBytes = hmac.ComputeHash(dataBytes);
        signature = Convert.ToBase64String(hashBytes);
    }

    // --- Feedback visuel ---
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("\n[DÉTAILS TECHNIQUES]");
    Console.WriteLine($"Body JSON : {jsonBody}");
    Console.WriteLine($"X-Timestamp : {timestamp}");
    Console.WriteLine($"X-Signature : {signature}");
    Console.ResetColor();

    // --- Envoi ---
    var client = new RestClient(SERVER_URL);
    var request = new RestRequest("api/transactions", Method.Post);
    request.AddHeader("X-Signature", signature);
    request.AddHeader("X-Timestamp", timestamp);
    request.AddJsonBody(jsonBody);

    Console.WriteLine("\nEnvoi de la requête au serveur...");
    var response = await client.ExecuteAsync(request);

    if (response.IsSuccessful)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"RÉPONSE DU SERVEUR : {response.Content}");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ÉCHEC ({response.StatusCode}) : {response.Content}");
    }
    Console.ResetColor();

    Console.WriteLine("\nAppuyez sur 'Entrée' pour une nouvelle transaction ou 'Ctrl+C' pour quitter.");
    Console.ReadLine();
}
