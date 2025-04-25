using MediatR;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Application.Features.Admin.EventHandlers
{
    public class UserStatusChangedEventHandler : INotificationHandler<UserStatusChangedEvent>
    {
        private readonly ILogger<UserStatusChangedEventHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        
        public UserStatusChangedEventHandler(
            ILogger<UserStatusChangedEventHandler> logger,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }
        
        public async Task Handle(UserStatusChangedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "User {UserId} status changed from {OldStatus} to {NewStatus} by admin {AdminId}",
                notification.UserId, 
                notification.OldStatus, 
                notification.NewStatus, 
                notification.ChangedByAdminId);
            
            // Additional processing logic can be added here
            // This includes:
            // - Sending notifications to the user
            // - Triggering additional workflows
            // - Syncing with external systems
            
            await Task.CompletedTask;
        }
    }
} 