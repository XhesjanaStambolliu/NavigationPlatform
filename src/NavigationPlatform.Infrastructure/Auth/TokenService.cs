using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NavigationPlatform.Application.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace NavigationPlatform.Infrastructure.Auth
{
    public class TokenService : ITokenService
    {
        private const string AccessTokenCookieName = "access_token";
        private const string RefreshTokenCookieName = "refresh_token";
        
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuth0Service _auth0Service;
        private readonly OpenApi.Auth0Settings _auth0Settings;
        private readonly ILogger<TokenService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        
        public TokenService(
            IHttpContextAccessor httpContextAccessor,
            IAuth0Service auth0Service,
            IOptions<OpenApi.Auth0Settings> auth0Settings,
            ILogger<TokenService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _auth0Service = auth0Service;
            _auth0Settings = auth0Settings.Value;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();
        }
        
        public async Task<bool> ValidateAccessTokenAsync()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                var accessToken = GetAccessTokenFromCookie();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogInformation("Access token is missing. Correlation ID: {CorrelationId}", correlationId);
                    return false;
                }
                
                var tokenValidationParameters = await GetTokenValidationParametersAsync();
                
                try
                {
                    var principal = _tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out var validatedToken);
                    
                    if (validatedToken.ValidTo > DateTime.UtcNow)
                    {
                        _logger.LogDebug("Access token is valid. Correlation ID: {CorrelationId}", correlationId);
                        return true;
                    }
                    
                    _logger.LogDebug("Access token has expired. Correlation ID: {CorrelationId}", correlationId);
                    return false;
                }
                catch (SecurityTokenExpiredException)
                {
                    _logger.LogDebug("Access token has expired. Correlation ID: {CorrelationId}", correlationId);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Token validation failed. Correlation ID: {CorrelationId}", correlationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating access token. Correlation ID: {CorrelationId}", correlationId);
                return false;
            }
        }
        
        public async Task<bool> RefreshAccessTokenAsync()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                var refreshToken = GetRefreshTokenFromCookie();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogInformation("Refresh token is missing. Correlation ID: {CorrelationId}", correlationId);
                    return false;
                }
                
                _logger.LogInformation("Attempting to refresh access token. Correlation ID: {CorrelationId}", correlationId);
                
                var tokenResult = await _auth0Service.RefreshAccessTokenAsync(refreshToken);
                
                if (!tokenResult.IsSuccess || string.IsNullOrEmpty(tokenResult.AccessToken))
                {
                    _logger.LogInformation("Token refresh failed. Correlation ID: {CorrelationId}", correlationId);
                    return false;
                }
                
                // Calculate refresh token expiry (typically longer than access token)
                var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(30); // Auth0 refresh tokens are valid for 30 days by default
                
                await StoreTokensInCookiesAsync(
                    tokenResult.AccessToken,
                    tokenResult.RefreshToken,
                    tokenResult.ExpiresAt,
                    refreshTokenExpiresAt);
                
                _logger.LogInformation("Token refresh successful. Correlation ID: {CorrelationId}", correlationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing access token. Correlation ID: {CorrelationId}", correlationId);
                return false;
            }
        }
        
        public async Task StoreTokensInCookiesAsync(string accessToken, string refreshToken, 
            DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                throw new InvalidOperationException("HttpContext is not available");
            }
            
            // Store access token in cookie
            _httpContextAccessor.HttpContext.Response.Cookies.Append(
                AccessTokenCookieName,
                accessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax, // Use Lax to allow redirects from Auth0
                    Expires = accessTokenExpiresAt,
                    Path = "/"
                });
            
            // Store refresh token in cookie (longer expiry)
            _httpContextAccessor.HttpContext.Response.Cookies.Append(
                RefreshTokenCookieName,
                refreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = refreshTokenExpiresAt,
                    Path = "/"
                });
            
            await Task.CompletedTask;
        }
        
        public async Task<ClaimsPrincipal?> GetUserPrincipalAsync()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                var accessToken = GetAccessTokenFromCookie();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogDebug("No access token found for user principal. Correlation ID: {CorrelationId}", correlationId);
                    return null;
                }
                
                var tokenValidationParameters = await GetTokenValidationParametersAsync();
                
                var principal = _tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user principal. Correlation ID: {CorrelationId}", correlationId);
                return null;
            }
        }
        
        public async Task ClearTokensAsync()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Clearing token cookies. Correlation ID: {CorrelationId}", correlationId);
                
                var httpContext = _httpContextAccessor.HttpContext;
                
                if (httpContext != null)
                {
                    // Delete the access token cookie
                    httpContext.Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });
                    
                    // Delete the refresh token cookie
                    httpContext.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });
                    
                    _logger.LogInformation("Token cookies successfully cleared. Correlation ID: {CorrelationId}", correlationId);
                }
                else
                {
                    _logger.LogWarning("No HttpContext available to clear token cookies. Correlation ID: {CorrelationId}", correlationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing token cookies. Correlation ID: {CorrelationId}", correlationId);
            }
            
            await Task.CompletedTask;
        }
        
        public string? GetRefreshToken()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            if (httpContext != null && httpContext.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken))
            {
                return refreshToken;
            }
            
            return null;
        }
        
        private async Task<TokenValidationParameters> GetTokenValidationParametersAsync()
        {
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"https://{_auth0Settings.Domain}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());
            
            var openIdConfig = await configManager.GetConfigurationAsync();
            
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://{_auth0Settings.Domain}/",
                ValidateAudience = true,
                ValidAudience = _auth0Settings.Audience,
                ValidateLifetime = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        }
        
        private string? GetAccessTokenFromCookie()
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return null;
            }
            
            return _httpContextAccessor.HttpContext.Request.Cookies[AccessTokenCookieName];
        }
        
        private string? GetRefreshTokenFromCookie()
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return null;
            }
            
            return _httpContextAccessor.HttpContext.Request.Cookies[RefreshTokenCookieName];
        }
    }
} 