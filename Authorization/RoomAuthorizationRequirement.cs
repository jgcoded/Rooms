
using Microsoft.AspNetCore.Authorization;

namespace p2p_api.Authorization;

public class RoomAuthorizationRequirement : IAuthorizationRequirement
{
    public const string Name = "RoomAuthorization";
}
