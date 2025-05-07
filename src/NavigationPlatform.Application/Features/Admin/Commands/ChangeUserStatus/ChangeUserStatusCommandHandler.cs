using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Application.Features.Admin.Commands.ChangeUserStatus
{
    public class ChangeUserStatusCommandHandler : IRequestHandler<ChangeUserStatusCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<ChangeUserStatusCommandHandler> _logger;
        private readonly IPublisher _publisher;
        private readonly ICurrentUserService _currentUserService;

        public ChangeUserStatusCommandHandler(
            IApplicationDbContext dbContext,
            ILogger<ChangeUserStatusCommandHandler> logger,
            IPublisher publisher,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisher = publisher;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<bool>> Handle(ChangeUserStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _dbContext.Users
                    .FindAsync(new object[] { request.UserId }, cancellationToken);

                if (user == null)
                {
                    return ApiResponse<bool>.CreateFailure("User not found");
                }

                // Record the old status for the audit
                var oldStatus = user.Status;
                
                // Update status
                user.Status = request.Status;
                
                // Create audit record
                var audit = new UserStatusAudit
                {
                    UserId = user.Id,
                    OldStatus = oldStatus,
                    NewStatus = request.Status,
                    ChangedByUserId = request.AdminId,
                    Reason = request.Reason,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _dbContext.UserStatusAudits.AddAsync(audit, cancellationToken);
                
                // Publish the event
                var @event = new UserStatusChangedEvent(
                    user.Id,
                    oldStatus,
                    request.Status,
                    request.AdminId);
                
                await _publisher.Publish(@event, cancellationToken);
                
                // Save changes
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                return ApiResponse<bool>.CreateSuccess(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user status");
                return ApiResponse<bool>.CreateFailure("An error occurred while changing the user status");
            }
        }
    }
} 