
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Text;
using System.Text.Encodings.Web;
 

namespace p2p_api.Authentication;

public class SSEAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{

    public SSEAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
            : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        throw new NotImplementedException();
    }
}
