
namespace p2p_api.Extensions;

using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static string UserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    public static string Country(this ClaimsPrincipal user)
    {
        const string Country = "country";
        return user.FindFirst(Country)?.Value ?? throw new UnauthorizedAccessException();
    }
}
