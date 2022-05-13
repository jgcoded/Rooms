
namespace p2p_api.Authorization;

using Microsoft.AspNetCore.Authorization;

public class UserInRoomRequirement : IAuthorizationRequirement
{
    public const string UserInRoom = "UserInRoom";
}
