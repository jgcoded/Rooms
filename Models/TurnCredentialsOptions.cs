
namespace p2p_api.Models;

public class TurnCredentialsOptions
{
    public const string TurnCredentials = "TurnCredentials";

    public string DatabaseConnectionString { get; set; } = string.Empty;

    public int CredentialsTTLSeconds { get; set; }

    public IList<string> ServerUris { get; set; } = new List<string>();
}
