
namespace Rooms.Models;

public class RoomTokenOptions
{
    public const string RoomToken = "RoomToken";

    public string Issuer { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public int TokenLifetimeSeconds;

}