
using Microsoft.AspNetCore.Authorization;

namespace p2p_api.Authorization;

public class SSEAuthorizationHandler : AuthorizationHandler<SSEAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SSEAuthorizationRequirement requirement)
    {
        // Resource is guaranteed to be HttpContext in Asp.NET
        var httpContext = context.Resource as HttpContext ?? throw new Exception();
        string? desiredRoom = httpContext.Request.RouteValues["roomName"] as string;

        if (desiredRoom is not null && context.User.HasClaim("RoomName", desiredRoom) )
        {
            context.Fail();
            return Task.CompletedTask;
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
