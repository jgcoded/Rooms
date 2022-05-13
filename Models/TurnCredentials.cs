namespace Rooms.Models;

public class TurnCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TTL { get; set; }
    public IList<string> Uris { get; set; } = new List<string>();
}
