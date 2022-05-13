
using Microsoft.AspNetCore.Authorization;

namespace Rooms.Authorization;

public class RoomAuthorizationRequirement : IAuthorizationRequirement
{
    public const string Name = "RoomAuthorization";
}
