
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authentication;

using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
 

using p2p_api.Extensions;
using p2p_api.Models;

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

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // extract the token
        if (!this.Request.Query.TryGetValue("t", out StringValues values))
        {
            return AuthenticateResult.Fail("Room token is missing");
        }

        if (values.Count > 1)
        {
            return AuthenticateResult.Fail("Only one token must be specified");
        }

        string token = values.Single();

        // decrypt the token
        RoomToken? roomToken = null;
        try
        {
            roomToken = await token.DecryptToRoomToken();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(exception: ex, "SSEAuthenticationHandler");
            return AuthenticateResult.Fail("Unable to decrypt room token");
        }

        if (roomToken is null)
        {
            return AuthenticateResult.Fail("Invalid room token");
        }

        // validate expiry
        if (DateTime.UtcNow > roomToken.Expiry)
        {
            return AuthenticateResult.Fail("Expired room token");
        }

    /*    claimsIdentity.AddClaims(
            new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, roomToken.UserId),
                new Claim(ClaimTypes.Role, "RoomGuest"),
                new Claim(nameof(roomToken.RoomName), roomToken.RoomName),
                new Claim(ClaimTypes.Expiration, roomToken.Expiry.ToString())
            }
        );*/

        var claimsPrincipal = new ClaimsPrincipal();
        var claimsIdentity = new ClaimsIdentity("JWT");
        claimsPrincipal.AddIdentity(claimsIdentity);

        var ticket = new AuthenticationTicket(
            claimsPrincipal,
            this.Scheme.Name
        );

        var s = this.Request.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return base.HandleChallengeAsync(properties);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        return base.HandleForbiddenAsync(properties);
    }
}
