using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Auth;


namespace NavigationPlatform.Infrastructure.Auth
{
    // This middleware is now simplified and does not automatically refresh tokens
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenRefreshMiddleware> _logger;
        
        public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
        {
            try
            {
                // Just pass through to the next middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(ex, "Error in middleware. Correlation ID: {CorrelationId}", correlationId);
                
                // Don't expose error details to client
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    error = "An error occurred processing the request",
                    correlationId
                });
            }
        }
    }
} 