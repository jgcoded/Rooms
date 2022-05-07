using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using p2p_api.Models;

namespace p2p_api.Controllers;

[ApiController]
[Route("")]
public class CredentialsController : ControllerBase
{
    private readonly TimeSpan ttl;

    private readonly IList<string> serverUris;

    private readonly ILogger<CredentialsController> logger;

    private readonly ReferenceHolder databaseSecret;

    public CredentialsController(ReferenceHolder databaseSecret, IOptions<CredentialsOptions> credentialsOptions, ILogger<CredentialsController> logger)
    {
        this.databaseSecret = databaseSecret;
        var options = credentialsOptions.Value;
        this.ttl = TimeSpan.FromSeconds(options.CredentialsTTLSeconds);
        this.serverUris = options.ServerUris;
        this.logger = logger;
    }

    [Authorize]
    [HttpGet(Name = "GetCredentials")]
    public Credentials Get()
    {
        string id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
        const string Country = "country";
        string country = User.FindFirst(Country)?.Value ?? throw new UnauthorizedAccessException();

        if (!string.Equals(country, "United States", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new UnauthorizedAccessException();
        }

        while (databaseSecret.Data.Length == 0)
        {
            Task.Delay(10).Wait();
        }

        int expiryTime = (int)(DateTime.UtcNow - DateTime.UnixEpoch).Add(this.ttl).TotalSeconds;
        string username = $"{expiryTime}:{id}";

        var secretBytes = Encoding.ASCII.GetBytes(databaseSecret.Data);
        var userBytes = Encoding.ASCII.GetBytes(username);

        var hmacSha = new HMACSHA1(secretBytes);
        var signatureBytes = hmacSha.ComputeHash(userBytes);
        string signature = System.Convert.ToBase64String(signatureBytes);

        return new Credentials
        {
            Username = username,
            Password = signature,
            TTL = (int)ttl.TotalSeconds,
            Uris = this.serverUris
        };
    }
}
