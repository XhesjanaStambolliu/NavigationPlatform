using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Application.Features.Journeys.Commands.GeneratePublicLink
{
    public class GeneratePublicLinkCommand : IRequest<ApiResponse<string>>
    {
        public Guid JourneyId { get; set; }
    }

    public class GeneratePublicLinkCommandValidator : AbstractValidator<GeneratePublicLinkCommand>
    {
        public GeneratePublicLinkCommandValidator()
        {
            RuleFor(x => x.JourneyId)
                .NotEmpty().WithMessage("Journey ID is required");
        }
    }

    public class GeneratePublicLinkCommandHandler : IRequestHandler<GeneratePublicLinkCommand, ApiResponse<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GeneratePublicLinkCommandHandler> _logger;

        public GeneratePublicLinkCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GeneratePublicLinkCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<ApiResponse<string>> Handle(GeneratePublicLinkCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            // Validate journey exists and current user is the owner
            var journey = await _context.Journeys
                .Include(j => j.PublicLinks.Where(pl => !pl.IsDisabled))
                .FirstOrDefaultAsync(j => j.Id == request.JourneyId && !j.IsDeleted, cancellationToken);

            if (journey == null)
            {
                throw new NotFoundException("Journey not found");
            }

            if (journey.OwnerId != currentUserId)
            {
                throw new ForbiddenAccessException("Only the journey owner can generate a public link");
            }

            // Check if there's an active public link, return it if it exists
            var existingPublicLink = journey.PublicLinks.FirstOrDefault();
            if (existingPublicLink != null)
            {
                return ApiResponse<string>.CreateSuccess(
                    existingPublicLink.Token,
                    "Existing public link retrieved"
                );
            }

            // Generate a new public link
            var token = Guid.NewGuid().ToString();
            var publicLink = new PublicLink
            {
                JourneyId = request.JourneyId,
                Token = token,
                IsDisabled = false,
                AccessCount = 0
            };

            _context.PublicLinks.Add(publicLink);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} generated a public link for journey {JourneyId}", 
                currentUserId, request.JourneyId);

            return ApiResponse<string>.CreateSuccess(
                token,
                "Public link generated successfully"
            );
        }
    }
} 