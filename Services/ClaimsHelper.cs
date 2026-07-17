using System.Security.Claims;

namespace QuickParkAPI.Services
{
    /// <summary>
    /// .NET's JWT middleware maps short claim names like "role" to long URI form
    /// (http://schemas.microsoft.com/ws/...). This helper reads both forms.
    /// </summary>
    public static class ClaimsHelper
    {
        // The long URIs that .NET maps to short names automatically
        private const string RoleUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        private const string NameIdUri = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

        public static string GetRole(ClaimsPrincipal user)
        {
            return user.Claims.FirstOrDefault(c => c.Type == "role")?.Value
                ?? user.Claims.FirstOrDefault(c => c.Type == RoleUri)?.Value
                ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                ?? string.Empty;
        }

        public static int GetUserId(ClaimsPrincipal user)
        {
            var val = user.Claims.FirstOrDefault(c => c.Type == "id")?.Value
                   ?? user.Claims.FirstOrDefault(c => c.Type == NameIdUri)?.Value
                   ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }

        public static string GetName(ClaimsPrincipal user)
        {
            return user.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                ?? "Anonymous User";
        }
    }
}
