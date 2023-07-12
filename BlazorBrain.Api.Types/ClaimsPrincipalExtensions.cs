using System.Security.Claims;

namespace BlazorBrain.Api.Types;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        var claim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null) throw new InvalidOperationException();
        return claim.Value;
    }
    
}