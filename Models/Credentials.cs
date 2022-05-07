namespace p2p_api.Models;

public class Credentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TTL { get; set; }
    public IList<string> Uris { get; set; } = new List<string>();
}
