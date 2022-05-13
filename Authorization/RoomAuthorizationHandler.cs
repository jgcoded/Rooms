
using Microsoft.AspNetCore.Authorization;

using Rooms.Models;
using Rooms.Services;

namespace Rooms.Authorization;

public class RoomAuthorizationHandler : AuthorizationHandler<RoomAuthorizationRequirement>
{
    private readonly RoomService roomsService;
    private readonly IHttpContextAccessor httpContextAccessor;
    public RoomAuthorizationHandler(RoomService roomsService, IHttpContextAccessor httpContextAccessor)
    {
        this.roomsService = roomsService;
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoomAuthorizationRequirement requirement)
    {
        if (this.httpContextAccessor.HttpContext is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var httpContext = httpContextAccessor.HttpContext;

        string? desiredRoom = httpContext.Request.RouteValues[nameof(RoomClaims.RoomId)] as string;
        if (desiredRoom is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(nameof(RoomClaims.RoomId), desiredRoom))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
