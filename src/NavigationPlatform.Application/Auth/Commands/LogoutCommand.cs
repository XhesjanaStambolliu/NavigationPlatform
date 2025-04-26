using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NavigationPlatform.Application.Auth.Commands
{
    public class LogoutCommand : IRequest<LogoutResult>
    {
        // No properties needed as we'll get the refresh token from the token service
    }
    
    public class LogoutResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool TokenRevoked { get; set; }
        public bool TokensCleared { get; set; }
    }
    
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResult>
    {
        private readonly ITokenService _tokenService;
        private readonly IAuth0Service _auth0Service;
        private readonly ILogger<LogoutCommandHandler> _logger;
        
        public LogoutCommandHandler(
            ITokenService tokenService,
            IAuth0Service auth0Service,
            ILogger<LogoutCommandHandler> logger)
        {
            _tokenService = tokenService;
            _auth0Service = auth0Service;
            _logger = logger;
        }
        
        public async Task<LogoutResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var correlationId = Guid.NewGuid().ToString();
            var result = new LogoutResult();
            
            try
            {
                // Get the current refresh token
                var refreshToken = _tokenService.GetRefreshToken();
                
                // If refresh token exists, revoke it with Auth0
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    result.TokenRevoked = await _auth0Service.RevokeRefreshTokenAsync(refreshToken);
                    
                    if (!result.TokenRevoked)
                    {
                        _logger.LogWarning("Failed to revoke refresh token with Auth0. Correlation ID: {CorrelationId}", correlationId);
                        result.ErrorMessage = "Failed to revoke refresh token with the authentication server. Your session may still be active on some devices.";
                    }
                }
                else
                {
                    _logger.LogWarning("No refresh token found for revocation. Correlation ID: {CorrelationId}", correlationId);
                    result.ErrorMessage = "No active session token found.";
                }
                
                try
                {
                    // Clear the tokens from cookies regardless of revocation success
                    await _tokenService.ClearTokensAsync();
                    result.TokensCleared = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clear token cookies. Correlation ID: {CorrelationId}", correlationId);
                    result.TokensCleared = false;
                    
                    if (result.ErrorMessage == null)
                    {
                        result.ErrorMessage = "Failed to clear local session data.";
                    }
                    else
                    {
                        result.ErrorMessage += " Additionally, failed to clear local session data.";
                    }
                }
                
                // Success if either we had no token to revoke OR we successfully revoked it AND we cleared cookies
                result.Success = (string.IsNullOrEmpty(refreshToken) || result.TokenRevoked) && result.TokensCleared;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout. Correlation ID: {CorrelationId}", correlationId);
                return new LogoutResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during logout.",
                    TokenRevoked = false,
                    TokensCleared = false
                };
            }
        }
    }
} 