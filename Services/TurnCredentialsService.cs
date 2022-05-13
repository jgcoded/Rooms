
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

using Rooms.Models;

namespace Rooms.Services;

public class TurnCredentialsService
{
    private readonly TimeSpan ttl;

    private readonly IList<string> serverUris;

    private readonly ReferenceHolder databaseSecret;

    public TurnCredentialsService(ReferenceHolder databaseSecret, IOptions<TurnCredentialsOptions> credentialsOptions)
    {
        this.databaseSecret = databaseSecret;
        var options = credentialsOptions.Value;
        this.ttl = TimeSpan.FromSeconds(options.CredentialsTTLSeconds);
        this.serverUris = options.ServerUris;
    }

    public TurnCredentials CreateTurnCredentials(string userId)
    {
        while (databaseSecret.Data.Length == 0)
        {
            Task.Delay(10).Wait();
        }

        int expiryTime = (int)(DateTime.UtcNow - DateTime.UnixEpoch).Add(this.ttl).TotalSeconds;
        string username = $"{expiryTime}:{userId}";

        string signature = HashSHA1(username, this.databaseSecret.Data);

        return new TurnCredentials
        {
            Username = username,
            Password = signature,
            TTL = (int)ttl.TotalSeconds,
            Uris = this.serverUris
        };
    }

    private static string HashSHA1(string data, string secret)
    {
        var secretBytes = Encoding.ASCII.GetBytes(secret);
        var dataBytes = Encoding.ASCII.GetBytes(data);
        var hmacSha = new HMACSHA1(secretBytes);
        var signatureBytes = hmacSha.ComputeHash(dataBytes);
        return Convert.ToBase64String(signatureBytes);
    }
}

