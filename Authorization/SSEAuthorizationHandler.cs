
using Microsoft.AspNetCore.Authorization;

namespace p2p_api.Authorization;

public class SSEAuthorizationHandler : AuthorizationHandler<SSEAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SSEAuthorizationRequirement requirement)
    {
        return Task.CompletedTask;
    }
}
