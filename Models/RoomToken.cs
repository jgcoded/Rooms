
namespace p2p_api.Models;

public class RoomToken
{
    public string RoomName { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public DateTime Expiry { get; set; } = DateTime.MinValue;
}