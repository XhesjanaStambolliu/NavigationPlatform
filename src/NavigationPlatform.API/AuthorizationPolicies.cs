using Microsoft.AspNetCore.Authorization;
using NavigationPlatform.API.Extensions;
using NavigationPlatform.Infrastructure.Auth;

namespace NavigationPlatform.API
{
    public static class AuthorizationPolicies
    {
        public const string AdminPolicy = "AdminPolicy";
        public const string JourneyOwnerOrSharedPolicy = "JourneyOwnerOrShared";

        public static void AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AdminPolicy, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context => 
                        context.User.IsAdmin());
                });
                
                options.AddPolicy(JourneyOwnerOrSharedPolicy, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new JourneyAuthorizationRequirement());
                });
            });
        }
    }
} 