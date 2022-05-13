
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Primitives;

using Rooms.Models;

namespace Rooms.Services;

public class RoomTokenService
{
    private const string TokenQueryParam = "t";

    private string issuer;

    private int tokenLifetimeSeconds;

    private SigningCredentials signingCredentials;

    public static Func<MessageReceivedContext, Task> TokenInUrlOnMessageReceived = (MessageReceivedContext context) =>
    {
        if (!context.Request.Path.StartsWithSegments("/Rooms"))
        {
            return Task.CompletedTask;
        }

        if (!context.Request.Query.TryGetValue(TokenQueryParam, out StringValues values))
        {
            return Task.CompletedTask;
        }

        if (values.Count > 1)
        {
            context.Fail("Only one token can be provided");
            return Task.CompletedTask;
        }

        var token = values.Single();

        if (String.IsNullOrWhiteSpace(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Fail("Token required");

            return Task.CompletedTask;
        }

        context.Token = token;

        return Task.CompletedTask;
    };

    public RoomTokenService(IOptions<RoomTokenOptions> options)
    {
        this.issuer = options.Value.Issuer;
        this.tokenLifetimeSeconds = options.Value.TokenLifetimeSeconds;
        this.signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key)),
            SecurityAlgorithms.HmacSha256Signature
        );
    }

    public string CreateJwt(RoomClaims roomToken)
    {
        var claims = new List<Claim>() {
            new Claim(ClaimTypes.NameIdentifier, roomToken.UserId),
            new Claim(ClaimTypes.Role, roomToken.UserRole),
            new Claim(nameof(roomToken.RoomId), roomToken.RoomId.ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: this.issuer,
            audience: this.issuer,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddSeconds(this.tokenLifetimeSeconds),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public static QueryString GetTokenQueryParameter(string encodedPayload)
    {
        return QueryString.Create(TokenQueryParam, encodedPayload);
    }
}
