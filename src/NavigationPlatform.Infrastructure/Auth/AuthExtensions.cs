using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NavigationPlatform.Application.Auth;
using System.Security.Claims;

namespace NavigationPlatform.Infrastructure.Auth
{
    public static class AuthExtensions
    {
        public static IServiceCollection AddAuth0CookieAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Register HTTP client factory for Auth0 API calls
            services.AddHttpClient();
            
            // Register HttpContextAccessor for cookie access
            services.AddHttpContextAccessor();
            
            // Configure Auth0 settings from configuration
            services.Configure<NavigationPlatform.Infrastructure.OpenApi.Auth0Settings>(configuration.GetSection("Auth0"));
            
            // Register Auth0 services
            services.AddScoped<IAuth0Service, Auth0Service>();
            services.AddScoped<ITokenService, TokenService>();
            
            // Configure JWT Bearer authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var auth0Section = configuration.GetSection("Auth0");
                
                options.Authority = auth0Section["Authority"];
                options.Audience = auth0Section["Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.NameIdentifier,
                    ValidateLifetime = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
                
                // Allow the middleware to handle token validation
                options.SaveToken = false;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Don't extract tokens from Authorization header
                        // We'll use our cookie-based approach instead
                        return Task.CompletedTask;
                    }
                };
            });
            
            return services;
        }
        
        public static IApplicationBuilder UseTokenRefreshMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<TokenRefreshMiddleware>();
            return app;
        }
    }
} 