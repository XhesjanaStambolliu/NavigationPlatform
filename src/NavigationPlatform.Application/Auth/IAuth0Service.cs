using NavigationPlatform.Application.Auth.Models;
using System.Threading.Tasks;

namespace NavigationPlatform.Application.Auth
{
    public interface IAuth0Service
    {
        /// <summary>
        /// Exchanges an authorization code for tokens
        /// </summary>
        /// <param name="code">The authorization code from Auth0</param>
        /// <param name="redirectUri">The redirect URI used in the authorization request</param>
        /// <param name="codeVerifier">The PKCE code verifier used in the authorization request</param>
        /// <returns>The token exchange result</returns>
        Task<TokenExchangeResult> ExchangeCodeForTokensAsync(string code, string redirectUri, string codeVerifier);
        
        /// <summary>
        /// Refreshes an access token using a refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token</param>
        /// <returns>The token exchange result</returns>
        Task<TokenExchangeResult> RefreshAccessTokenAsync(string refreshToken);
        
        /// <summary>
        /// Revokes a refresh token with the Auth0 OIDC provider
        /// </summary>
        /// <param name="refreshToken">The refresh token to revoke</param>
        /// <returns>True if the token was successfully revoked, false otherwise</returns>
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
    }
} 