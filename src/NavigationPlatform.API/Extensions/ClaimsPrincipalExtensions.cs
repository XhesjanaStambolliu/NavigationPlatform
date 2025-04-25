using System.Security.Claims;
using System.Text.Json;

namespace NavigationPlatform.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Checks if the user has a specific scope
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal to check</param>
        /// <param name="scope">The scope to check for</param>
        /// <returns>True if the user has the scope, false otherwise</returns>
        public static bool HasScope(this ClaimsPrincipal principal, string scope)
        {
            if (principal == null)
                return false;

            var scopeClaim = principal.FindFirst("scope");
            if (scopeClaim == null)
                return false;

            // Check if the requested scope is in the space-separated scopes claim
            return scopeClaim.Value.Split(' ').Contains(scope);
        }

        /// <summary>
        /// Checks if the user has a specific permission in the permissions array
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal to check</param>
        /// <param name="permission">The permission to check for</param>
        /// <returns>True if the user has the permission, false otherwise</returns>
        public static bool HasPermission(this ClaimsPrincipal principal, string permission)
        {
            if (principal == null)
                return false;

            // Check for standard permissions claim
            var permissionsClaim = principal.FindFirst("permissions");
            
            // Also check for Auth0 specific format
            if (permissionsClaim == null)
                permissionsClaim = principal.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
                
            if (permissionsClaim == null)
                return false;

            try
            {
                // Try to deserialize as JSON array
                if (permissionsClaim.Value.StartsWith("[") && permissionsClaim.Value.EndsWith("]"))
                {
                    var permissions = JsonSerializer.Deserialize<string[]>(permissionsClaim.Value);
                    return permissions?.Contains(permission, StringComparer.OrdinalIgnoreCase) ?? false;
                }
                
                // Check if it's a single permission value
                return string.Equals(permissionsClaim.Value, permission, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // If we can't deserialize, check if the permission is in the raw string
                return permissionsClaim.Value.Contains(permission, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Checks if the user has Admin privileges through either scope or permissions
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal to check</param>
        /// <returns>True if the user has the Admin scope or permission, false otherwise</returns>
        public static bool IsAdmin(this ClaimsPrincipal principal)
        {
            // Check for Admin in scope
            if (principal.HasScope("Admin"))
                return true;
                
            // Check for Admin in permissions array (case-insensitive)
            if (principal.HasPermission("Admin"))
                return true;
                
            return false;
        }
    }
} 