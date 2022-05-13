using Lib.AspNetCore.ServerSentEvents;
using Rooms.Extensions;

namespace Rooms.Providers;

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