using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NavigationPlatform.Application.Auth
{
    public interface ITokenService
    {
        /// <summary>
        /// Validates the access token from the request
        /// </summary>
        /// <returns>True if the token is valid, false otherwise</returns>
        Task<bool> ValidateAccessTokenAsync();
        
        /// <summary>
        /// Refreshes the access token using the refresh token
        /// </summary>
        /// <returns>True if refresh was successful, false otherwise</returns>
        Task<bool> RefreshAccessTokenAsync();
        
        /// <summary>
        /// Stores the access and refresh tokens in HTTP-only cookies
        /// </summary>
        /// <param name="accessToken">The access token to store</param>
        /// <param name="refreshToken">The refresh token to store</param>
        /// <param name="accessTokenExpiresAt">When the access token expires</param>
        /// <param name="refreshTokenExpiresAt">When the refresh token expires</param>
        Task StoreTokensInCookiesAsync(string accessToken, string refreshToken, 
            DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt);
        
        /// <summary>
        /// Gets the principal from the current valid token
        /// </summary>
        /// <returns>ClaimsPrincipal from the token or null if invalid</returns>
        Task<ClaimsPrincipal?> GetUserPrincipalAsync();
        
        /// <summary>
        /// Clears all token cookies as part of the logout process
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task ClearTokensAsync();
        
        /// <summary>
        /// Gets the current refresh token from the cookies
        /// </summary>
        /// <returns>The refresh token or null if not found</returns>
        string? GetRefreshToken();
    }
} 