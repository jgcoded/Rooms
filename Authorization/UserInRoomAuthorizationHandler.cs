

using Microsoft.AspNetCore.Authorization;

using p2p_api.Extensions;
using p2p_api.Services;

namespace p2p_api.Authorization;


using Microsoft.AspNetCore.Authorization;

public class UserInRoomRequirement : IAuthorizationRequirement
{
}

public class UserInRoomAuthorizationHandler : AuthorizationHandler<UserInRoomRequirement>
{
    private readonly RoomsService roomsService;

    public UserInRoomAuthorizationHandler(RoomsService roomsService)
    {
        this.roomsService = roomsService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserInRoomRequirement requirement)
    {
        // Resource is guaranteed to be HttpContext in Asp.NET
        var httpContext = context.Resource as HttpContext ?? throw new Exception();
        string? desiredRoom = httpContext.Request.RouteValues["roomName"] as string;

        if (desiredRoom is not null && this.roomsService.IsUserInRoom(desiredRoom, context.User.UserId()) )
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        //context.Fail();
        return Task.CompletedTask;
    }
}
