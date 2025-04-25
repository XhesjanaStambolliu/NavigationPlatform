using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.API.Middleware
{
    public class UserStatusValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserStatusValidationMiddleware> _logger;
        
        public UserStatusValidationMiddleware(RequestDelegate next, ILogger<UserStatusValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            // Skip status validation for authentication endpoints
            if (context.Request.Path.StartsWithSegments("/api/auth"))
            {
                await _next(context);
                return;
            }
            
            // Only check authenticated users
            if (currentUserService.IsAuthenticated)
            {
                try
                {
                    // Try to resolve the user - this is needed because CurrentUserService caches the user
                    var userId = currentUserService.UserId;
                    
                    // Double-check the user status directly from the database
                    var user = await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userId);
                    
                    if (user != null && user.Status != UserStatus.Active)
                    {
                        // User is not active
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        
                        var errorMessage = user.Status switch
                        {
                            UserStatus.Suspended => "Your account has been suspended. Please contact an administrator.",
                            UserStatus.Deactivated => "Your account has been deactivated. Please contact an administrator.",
                            _ => "Your account is not active. Please contact an administrator."
                        };
                        
                        _logger.LogWarning("User {UserId} authentication rejected due to status: {Status}", userId, user.Status);
                        
                        var response = new
                        {
                            success = false,
                            message = errorMessage
                        };
                        
                        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating user status");
                }
            }
            
            await _next(context);
        }
    }
} 