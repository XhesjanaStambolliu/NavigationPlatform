using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NavigationPlatform.Application.Auth;
using NavigationPlatform.Application.Auth.Commands;
using NavigationPlatform.Application.Auth.Models;


namespace NavigationPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly ITokenService _tokenService;
        private readonly IAuth0Service _auth0Service;
        private readonly IMediator _mediator;
        
        public AuthController(
            ILogger<AuthController> logger,
            ITokenService _tokenService,
            IAuth0Service auth0Service,
            IMediator mediator)
        {
            _logger = logger;
            this._tokenService = _tokenService;
            _auth0Service = auth0Service;
            _mediator = mediator;
        }
        
        [HttpPost]
        [Route("/api/auth/refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    _logger.LogWarning("No refresh token provided. Correlation ID: {CorrelationId}", correlationId);
                    return BadRequest(new { error = "Refresh token is required" });
                }
                
                // Call Auth0 service to refresh token
                var result = await _auth0Service.RefreshAccessTokenAsync(request.RefreshToken);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Token refresh failed via API endpoint. Correlation ID: {CorrelationId}", correlationId);
                    return Unauthorized(new { error = result.Error, error_description = result.ErrorDescription });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh via API endpoint. Correlation ID: {CorrelationId}", correlationId);
                return StatusCode(500, new { error = "An error occurred during token refresh", correlationId });
            }
        }
        
        [HttpPost]
        [Route("/api/auth/logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                var command = new LogoutCommand();
                var result = await _mediator.Send(command);
                
                if (result.Success)
                {
                    return Ok(new { message = "Successfully logged out" });
                }
                else
                {
                    _logger.LogWarning("Logout completed with warnings via API endpoint. Error: {Error}, Correlation ID: {CorrelationId}", 
                        result.ErrorMessage, correlationId);
                    return StatusCode(207, new { 
                        message = "Logout completed with warnings",
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout via API endpoint. Correlation ID: {CorrelationId}", correlationId);
                return StatusCode(500, new { error = "An error occurred during logout", correlationId });
            }
        }
    }
} 