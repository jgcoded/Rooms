

using Microsoft.AspNetCore.Authorization;

using p2p_api.Extensions;
using p2p_api.Models;
using p2p_api.Services;

namespace p2p_api.Authorization;

public class UserInRoomAuthorizationHandler : AuthorizationHandler<UserInRoomRequirement>
{
    private readonly RoomService roomsService;

    public UserInRoomAuthorizationHandler(RoomService roomsService)
    {
        this.roomsService = roomsService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserInRoomRequirement requirement)
    {
        // Resource is guaranteed to be HttpContext in Asp.NET
        var httpContext = context.Resource as HttpContext ?? throw new Exception();
        string? roomId = httpContext.Request.RouteValues[nameof(RoomClaims.RoomId)] as string;

        if (string.IsNullOrWhiteSpace(roomId))
        {
            return Task.CompletedTask;
        }

        if (this.roomsService.IsUserInRoom(Guid.Parse(roomId), context.User.UserId()))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
