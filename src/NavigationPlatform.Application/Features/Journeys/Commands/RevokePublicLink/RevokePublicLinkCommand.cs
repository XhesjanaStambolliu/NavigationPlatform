using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;

namespace NavigationPlatform.Application.Features.Journeys.Commands.RevokePublicLink
{
    public class RevokePublicLinkCommand : IRequest<ApiResponse>
    {
        public Guid JourneyId { get; set; }
    }

    public class RevokePublicLinkCommandValidator : AbstractValidator<RevokePublicLinkCommand>
    {
        public RevokePublicLinkCommandValidator()
        {
            RuleFor(x => x.JourneyId)
                .NotEmpty().WithMessage("Journey ID is required");
        }
    }

    public class RevokePublicLinkCommandHandler : IRequestHandler<RevokePublicLinkCommand, ApiResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RevokePublicLinkCommandHandler> _logger;

        public RevokePublicLinkCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<RevokePublicLinkCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<ApiResponse> Handle(RevokePublicLinkCommand request, CancellationToken cancellationToken)
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
                throw new ForbiddenAccessException("Only the journey owner can revoke a public link");
            }

            // Find active public links for this journey
            var publicLinks = await _context.PublicLinks
                .Where(pl => pl.JourneyId == request.JourneyId && !pl.IsDisabled)
                .ToListAsync(cancellationToken);

            if (!publicLinks.Any())
            {
                return ApiResponse.CreateFailure("No active public links found for this journey");
            }

            // Mark all active links as disabled
            foreach (var link in publicLinks)
            {
                link.IsDisabled = true;
                link.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.CreateSuccess($"Successfully revoked {publicLinks.Count} public links");
        }
    }
} 