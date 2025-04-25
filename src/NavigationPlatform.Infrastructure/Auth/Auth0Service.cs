using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NavigationPlatform.Application.Auth;
using NavigationPlatform.Application.Auth.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace NavigationPlatform.Infrastructure.Auth
{
    public class Auth0Service : IAuth0Service
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly NavigationPlatform.Infrastructure.OpenApi.Auth0Settings _auth0Settings;
        private readonly ILogger<Auth0Service> _logger;
        
        public Auth0Service(
            IHttpClientFactory httpClientFactory,
            IOptions<NavigationPlatform.Infrastructure.OpenApi.Auth0Settings> auth0Settings,
            ILogger<Auth0Service> logger)
        {
            _httpClientFactory = httpClientFactory;
            _auth0Settings = auth0Settings.Value;
            _logger = logger;
        }
        
        public async Task<TokenExchangeResult> ExchangeCodeForTokensAsync(string code, string redirectUri, string codeVerifier)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogDebug("Exchanging code for tokens. Correlation ID: {CorrelationId}", correlationId);
                
                var client = _httpClientFactory.CreateClient();
                var tokenEndpoint = $"https://{_auth0Settings.Domain}/oauth/token";
                
                var tokenRequest = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", _auth0Settings.ClientId },
                    { "client_secret", _auth0Settings.ClientSecret },
                    { "code", code },
                    { "redirect_uri", redirectUri },
                    { "code_verifier", codeVerifier }
                };
                
                // Use FormUrlEncodedContent for Auth0 token endpoint
                var formContent = new FormUrlEncodedContent(tokenRequest);
                var response = await client.PostAsync(tokenEndpoint, formContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogDebug("Token response received. Correlation ID: {CorrelationId}", correlationId);
                    
                    // Use JsonDocument for more flexible parsing
                    using var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;
                    
                    var tokenResponse = new TokenExchangeResult
                    {
                        IsSuccess = true
                    };
                    
                    // Extract properties one by one with proper error handling
                    if (root.TryGetProperty("access_token", out var accessTokenElement))
                        tokenResponse.AccessToken = accessTokenElement.GetString() ?? string.Empty;
                        
                    if (root.TryGetProperty("refresh_token", out var refreshTokenElement))
                        tokenResponse.RefreshToken = refreshTokenElement.GetString() ?? string.Empty;
                        
                    if (root.TryGetProperty("id_token", out var idTokenElement))
                        tokenResponse.IdToken = idTokenElement.GetString() ?? string.Empty;
                        
                    if (root.TryGetProperty("token_type", out var tokenTypeElement))
                        tokenResponse.TokenType = tokenTypeElement.GetString() ?? string.Empty;
                        
                    if (root.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.TryGetInt32(out int expiresIn))
                        tokenResponse.ExpiresIn = expiresIn;
                    else
                        tokenResponse.ExpiresIn = 3600; // Default to 1 hour if not provided
                        
                    if (root.TryGetProperty("scope", out var scopeElement))
                        tokenResponse.Scope = scopeElement.GetString() ?? string.Empty;
                    
                    _logger.LogInformation("Code exchange successful. Access token and refresh token obtained. Correlation ID: {CorrelationId}", correlationId);
                    return tokenResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Code exchange failed with status {StatusCode}. Error: {ErrorContent}. Correlation ID: {CorrelationId}", 
                        response.StatusCode, errorContent, correlationId);
                    
                    return new TokenExchangeResult
                    {
                        IsSuccess = false,
                        Error = "code_exchange_failed",
                        ErrorDescription = $"Failed to exchange code for tokens. Status code: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during code exchange. Correlation ID: {CorrelationId}", correlationId);
                
                return new TokenExchangeResult
                {
                    IsSuccess = false,
                    Error = "code_exchange_exception",
                    ErrorDescription = $"An exception occurred during code exchange"
                };
            }
        }
        
        public async Task<TokenExchangeResult> RefreshAccessTokenAsync(string refreshToken)
        {
            return await RefreshAccessTokenAsync(refreshToken, includeClientSecret: true);
        }
        
        /// <summary>
        /// Internal implementation of RefreshAccessTokenAsync with option to exclude client secret
        /// </summary>
        private async Task<TokenExchangeResult> RefreshAccessTokenAsync(string refreshToken, bool includeClientSecret)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Token refresh attempted. Correlation ID: {CorrelationId}", correlationId);
                
                var client = _httpClientFactory.CreateClient();
                var tokenEndpoint = $"https://{_auth0Settings.Domain}/oauth/token";
                
                var tokenRequest = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", _auth0Settings.ClientId },
                    { "refresh_token", refreshToken }
                };
                
                // Only include client_secret if explicitly requested
                if (includeClientSecret)
                {
                    tokenRequest.Add("client_secret", _auth0Settings.ClientSecret);
                }
                
                var formContent = new FormUrlEncodedContent(tokenRequest);
                var response = await client.PostAsync(tokenEndpoint, formContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Use JsonDocument for more flexible parsing
                    using var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;
                    
                    var tokenResponse = new TokenExchangeResult
                    {
                        IsSuccess = true
                    };
                    
                    // Extract properties one by one with proper error handling
                    if (root.TryGetProperty("access_token", out var accessTokenElement))
                        tokenResponse.AccessToken = accessTokenElement.GetString() ?? string.Empty;
                        
                    if (root.TryGetProperty("refresh_token", out var refreshTokenElement))
                        tokenResponse.RefreshToken = refreshTokenElement.GetString() ?? string.Empty;
                    else
                        tokenResponse.RefreshToken = refreshToken; // Keep existing refresh token if not provided
                        
                    if (root.TryGetProperty("id_token", out var idTokenElement))
                        tokenResponse.IdToken = idTokenElement.GetString() ?? string.Empty;
                        
                    if (root.TryGetProperty("token_type", out var tokenTypeElement))
                        tokenResponse.TokenType = tokenTypeElement.GetString() ?? string.Empty;
                        
                    if (root.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.TryGetInt32(out int expiresIn))
                        tokenResponse.ExpiresIn = expiresIn;
                    else
                        tokenResponse.ExpiresIn = 86400; // Default to 24 hours if not provided
                        
                    if (root.TryGetProperty("scope", out var scopeElement))
                        tokenResponse.Scope = scopeElement.GetString() ?? string.Empty;
                        
                    _logger.LogInformation("Token refresh successful. Correlation ID: {CorrelationId}", correlationId);
                    return tokenResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Token refresh failed. Status: {StatusCode}, Response: {Response}, Correlation ID: {CorrelationId}", 
                        response.StatusCode, errorContent, correlationId);
                    
                    var tokenResponse = new TokenExchangeResult { IsSuccess = false };
                    
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(errorContent);
                        var root = jsonDoc.RootElement;
                        
                        if (root.TryGetProperty("error", out var errorElement))
                            tokenResponse.Error = errorElement.GetString() ?? string.Empty;
                            
                        if (root.TryGetProperty("error_description", out var errorDescElement))
                            tokenResponse.ErrorDescription = errorDescElement.GetString() ?? string.Empty;
                    }
                    catch
                    {
                        // If parsing fails, use a generic error
                        tokenResponse.Error = "invalid_request";
                        tokenResponse.ErrorDescription = "Failed to refresh token";
                    }
                    
                    return tokenResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during token refresh. Correlation ID: {CorrelationId}", correlationId);
                return new TokenExchangeResult
                {
                    IsSuccess = false,
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred during token refresh"
                };
            }
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Revoking refresh token. Correlation ID: {CorrelationId}", correlationId);
                
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("No refresh token provided for revocation. Correlation ID: {CorrelationId}", correlationId);
                    return false;
                }
                
                var client = _httpClientFactory.CreateClient();
                var revocationEndpoint = $"https://{_auth0Settings.Domain}/oauth/revoke";
                
                var revocationRequest = new Dictionary<string, string>
                {
                    { "client_id", _auth0Settings.ClientId },
                    { "client_secret", _auth0Settings.ClientSecret },
                    { "token", refreshToken },
                    { "token_type_hint", "refresh_token" }
                };
                
                var formContent = new FormUrlEncodedContent(revocationRequest);
                var response = await client.PostAsync(revocationEndpoint, formContent);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Refresh token successfully revoked. Correlation ID: {CorrelationId}", correlationId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to revoke refresh token. Status: {StatusCode}, Response: {Response}, Correlation ID: {CorrelationId}", 
                        response.StatusCode, errorContent, correlationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token. Correlation ID: {CorrelationId}", correlationId);
                return false;
            }
        }
    }
} 