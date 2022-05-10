
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

        string token = values.ToString();

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

        var claimsIdentity = new ClaimsIdentity(
            new SSEIdentity(roomToken.UserId),
            new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, roomToken.UserId),
                new Claim(ClaimTypes.Role, "RoomGuest"),
                new Claim(nameof(roomToken.RoomName), roomToken.RoomName),
                new Claim(ClaimTypes.Expiration, roomToken.Expiry.ToString())
            }, authenticationType: "JWT", null, null);

        var claimsPrincipal = new ClaimsPrincipal();
        var identity = new ClaimsIdentity("JWT");
        identity.AddClaims(            new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, roomToken.UserId),
                new Claim(ClaimTypes.Role, "RoomGuest"),
                new Claim(nameof(roomToken.RoomName), roomToken.RoomName),
                new Claim(ClaimTypes.Expiration, roomToken.Expiry.ToString())
            });
        claimsPrincipal.AddIdentity(identity);

        var ticket = new AuthenticationTicket(
            claimsPrincipal,
            this.Scheme.Name
        );

        return AuthenticateResult.Success(ticket);
    }
}
