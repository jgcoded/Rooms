
using Microsoft.AspNetCore.Authentication;

namespace p2p_api.Authentication;

public class SSEAuthenticationScheme : AuthenticationScheme
{
    public const string SchemeName = "SSEAuthenticationScheme";
    public SSEAuthenticationScheme()
        : base(SchemeName, "SSE Authentication Scheme", typeof(SSEAuthenticationHandler))
    {
    }
}

