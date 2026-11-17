using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace RottenPotatoes.Api.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Den indloggede brugers Users.Id fra JWT'ens "sub"-claim.
    /// Bruges kun på endpoints med [Authorize], hvor tokenet allerede er valideret.
    /// </summary>
    public static ulong GetUserId(this ClaimsPrincipal user)
        => ulong.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Tokenet mangler 'sub'-claim."));
}
