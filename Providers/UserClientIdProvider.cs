using Lib.AspNetCore.ServerSentEvents;
using p2p_api.Extensions;

namespace p2p_api.Providers;

public class UserClientIdProvider : IServerSentEventsClientIdProvider
{
    public Guid AcquireClientId(HttpContext context)
    {
        return Guid.Parse(context.User.UserId());
    }

    public void ReleaseClientId(Guid clientId, HttpContext context)
    {
    }
}