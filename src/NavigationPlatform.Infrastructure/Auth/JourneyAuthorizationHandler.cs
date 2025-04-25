using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Infrastructure.Persistence;
using NavigationPlatform.Application.Common.Interfaces;

namespace NavigationPlatform.Infrastructure.Auth
{
    public class JourneyAuthorizationRequirement : IAuthorizationRequirement
    {
    }

    public class JourneyAuthorizationHandler : AuthorizationHandler<JourneyAuthorizationRequirement, Journey>
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<JourneyAuthorizationHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public JourneyAuthorizationHandler(
            AppDbContext dbContext, 
            ILogger<JourneyAuthorizationHandler> logger,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            JourneyAuthorizationRequirement requirement,
            Journey resource)
        {
            // Get the user ID from CurrentUserService instead of claims
            var userId = _currentUserService.UserId;
            
            // Log the current user ID and the journey owner ID for debugging
            _logger.LogInformation(
                "Authorization check: User ID: {UserId}, Journey Owner ID: {OwnerId}",
                userId, resource.OwnerId);
            
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("User ID is empty or invalid");
                return;
            }

            // Check if user is the owner
            if (resource.OwnerId == userId)
            {
                _logger.LogInformation("User is the owner of the journey - access granted");
                context.Succeed(requirement);
                return;
            }
            
            // Optional: Check if journey is shared with the user (if required by the spec)
            var isSharedWithUser = await _dbContext.JourneyShares
                .AnyAsync(js => js.JourneyId == resource.Id && js.UserId == userId);

            if (isSharedWithUser)
            {
                _logger.LogInformation("Journey is shared with the user - access granted");
                context.Succeed(requirement);
                return;
            }
            
            _logger.LogWarning("Access denied - user is not the owner and journey is not shared with them");
        }
    }
} 