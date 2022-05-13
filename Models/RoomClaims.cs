
namespace p2p_api.Models;

public class RoomClaims
{
    public Guid RoomId { get; set; } = Guid.Empty;

    public string UserId { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

}