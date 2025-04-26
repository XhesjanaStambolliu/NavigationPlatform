using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CurrentUserService> _logger;
        
        // Cache the user for the request lifetime
        private ApplicationUser _currentUser;
        private bool _userResolved;
        private string _userName;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor, 
            IServiceProvider serviceProvider,
            ILogger<CurrentUserService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Guid UserId
        {
            get
            {
                EnsureUserResolved();
                return _currentUser?.Id ?? Guid.Empty;
            }
        }

        public string UserName
        {
            get
            {
                EnsureUserResolved();
                return _userName;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            }
        }

        private void EnsureUserResolved()
        {
            if (_userResolved)
                return;

            if (!IsAuthenticated)
            {
                _userResolved = true;
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            
            // Get email from X-User-Email header instead of claims
            string email = httpContext.Request.Headers["X-User-Email"].FirstOrDefault();
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("X-User-Email header is missing");
                throw new ForbiddenAccessException("X-User-Email header is missing");
            }

            // Still get the username from claims if available
            var user = httpContext.User;
            _userName = user.FindFirstValue("nickname") ?? "Unknown";

            // Create a new scope to resolve the dbContext
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            
            // Find the user by email
            _currentUser = dbContext.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == email);

            if (_currentUser == null)
            {
                _logger.LogWarning("User with email {Email} not found in database", email);
                throw new ForbiddenAccessException($"User with email {email} not found");
            }

            _userResolved = true;
        }
    }
} 