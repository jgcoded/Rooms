
using System.Security.Principal;

namespace p2p_api.Authentication;

public class SSEIdentity : IIdentity
{
    private readonly string name;
    public SSEIdentity(string name)
    {
        this.name = name;
    }

    public string? AuthenticationType => "SSE";

    public bool IsAuthenticated => true;

    public string? Name => this.name;
}

public class SSEPrincipal : IPrincipal
{
    private readonly HashSet<string> roles;
    private readonly SSEIdentity identity;

    public SSEPrincipal(string userId, IList<string> roles)
    {
        this.roles = new HashSet<string>(roles);
        this.identity = new SSEIdentity(userId);
    }

    IIdentity? IPrincipal.Identity => this.identity;

    public bool IsInRole(string role)
    {
        return this.roles.Contains(role);
    }
}
