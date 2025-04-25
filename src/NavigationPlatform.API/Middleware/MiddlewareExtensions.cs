using Microsoft.AspNetCore.Builder;

namespace NavigationPlatform.API.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
        
        public static IApplicationBuilder UseUserStatusValidation(this IApplicationBuilder app)
        {
            return app.UseMiddleware<UserStatusValidationMiddleware>();
        }
    }
} 