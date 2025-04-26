using System;
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Application.Features.Journeys.Commands.ShareJourney
{
    public class ShareJourneyCommand : IRequest<ApiResponse>
    {
        public Guid JourneyId { get; set; }
        public List<Guid> UserIds { get; set; } = new List<Guid>();
        public string ShareNote { get; set; }
    }

    public class ShareJourneyCommandValidator : AbstractValidator<ShareJourneyCommand>
    {
        public ShareJourneyCommandValidator()
        {
            RuleFor(x => x.JourneyId)
                .NotEmpty().WithMessage("Journey ID is required");

            RuleFor(x => x.UserIds)
                .NotEmpty().WithMessage("At least one user ID is required");

            RuleFor(x => x.ShareNote)
                .MaximumLength(500).WithMessage("Share note must not exceed 500 characters");
        }
    }

    public class ShareJourneyCommandHandler : IRequestHandler<ShareJourneyCommand, ApiResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ShareJourneyCommandHandler> _logger;

        public ShareJourneyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<ShareJourneyCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<ApiResponse> Handle(ShareJourneyCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            // Validate journey exists and current user is the owner
            var journey = await _context.Journeys
                .FirstOrDefaultAsync(j => j.Id == request.JourneyId && !j.IsDeleted, cancellationToken);

            if (journey == null)
            {
                throw new NotFoundException("Journey not found");
            }

            if (journey.OwnerId != currentUserId)
            {
                throw new ForbiddenAccessException("Only the journey owner can share it");
            }

            // Validate all users exist
            var userIds = request.UserIds.Distinct().ToList();
            var existingUsers = await _context.Users
                .Where(u => userIds.Contains(u.Id) && u.Status == UserStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            var invalidUserIds = userIds.Except(existingUsers).ToList();
            if (invalidUserIds.Any())
            {
                return ApiResponse.CreateFailure($"The following user IDs are invalid or inactive: {string.Join(", ", invalidUserIds)}");
            }

            // Check for existing shares to prevent duplicates
            var existingShares = await _context.JourneyShares
                .Where(js => js.JourneyId == request.JourneyId && userIds.Contains(js.UserId))
                .Select(js => js.UserId)
                .ToListAsync(cancellationToken);

            var newShares = userIds.Except(existingShares).ToList();
            
            // Create new shares
            foreach (var userId in newShares)
            {
                var journeyShare = new JourneyShare
                {
                    JourneyId = request.JourneyId,
                    UserId = userId,
                    ShareType = ShareType.Direct,
                    ShareNote = request.ShareNote
                };

                _context.JourneyShares.Add(journeyShare);
                
                // Add audit for the share action
                var shareAudit = new ShareAudit
                {
                    JourneyShareId = journeyShare.Id,
                    Action = "Share",
                    Details = $"Shared by user {currentUserId} with user {userId}",
                    IpAddress = "N/A" // This would typically come from the request context
                };
                
                _context.ShareAudits.Add(shareAudit);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.CreateSuccess($"Journey shared with {newShares.Count} users");
        }
    }
} 