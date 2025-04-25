using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;


namespace NavigationPlatform.Infrastructure.OpenApi
{
    public static class SwaggerExtensions
    {
        // Hardcoded scopes as requested
        private static readonly string[] Scopes = new[] { "openid", "profile", "email", "read:data", "offline_access" };

        public static IServiceCollection AddSwaggerWithAuth0(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Auth0 settings from configuration
            services.Configure<Auth0Settings>(configuration.GetSection("Auth0"));
            
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Navigation API", Version = "v1" });
                
                // Get Auth0 settings
                var serviceProvider = services.BuildServiceProvider();
                var auth0Settings = serviceProvider.GetRequiredService<IOptions<Auth0Settings>>().Value;
                
                // Use hardcoded scopes with fixed descriptions
                var scopesDictionary = new Dictionary<string, string>
                {
                    { "openid", "OpenID" },
                    { "profile", "User profile" },
                    { "email", "User email" },
                    { "read:data", "Access API data" },
                    { "offline_access", "Refresh Token" }
                };

                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"https://{auth0Settings.Domain}/authorize"),
                            TokenUrl = new Uri($"https://{auth0Settings.Domain}/oauth/token"),
                            Scopes = scopesDictionary
                        }
                    }
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "oauth2"
                            }
                        },
                        Scopes
                    }
                });
                
                // Add custom operation filter to include X-User-Email header
                c.OperationFilter<AddEmailHeaderOperationFilter>();
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerWithAuth0(this IApplicationBuilder app)
        {
            // Get Auth0 settings from the application's service provider
            var auth0Settings = app.ApplicationServices.GetRequiredService<IOptions<Auth0Settings>>().Value;

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Navigation Platform API");

                // Apply Auth0 client credentials from configuration
                c.OAuthClientId(auth0Settings.ClientId);
                c.OAuthClientSecret(auth0Settings.ClientSecret);
                
                // Enable PKCE
                c.OAuthUsePkce();
                c.OAuthAppName("Navigation Platform Swagger UI");
                
                // Add audience parameter
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
                {
                    { "audience", auth0Settings.Audience }
                });

                // Use space as scope separator for Auth0
                c.OAuthScopeSeparator(" ");
                
                // Configure OAuth receive endpoint
                c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            });

            return app;
        }
    }
} 