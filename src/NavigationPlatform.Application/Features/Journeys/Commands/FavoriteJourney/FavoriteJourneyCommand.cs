using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Application.Features.Journeys.Commands.FavoriteJourney
{
    public class FavoriteJourneyCommand : IRequest<ApiResponse>
    {
        public Guid JourneyId { get; set; }
    }

    public class FavoriteJourneyCommandValidator : AbstractValidator<FavoriteJourneyCommand>
    {
        public FavoriteJourneyCommandValidator()
        {
            RuleFor(x => x.JourneyId)
                .NotEmpty().WithMessage("Journey ID is required.");
        }
    }

    public class FavoriteJourneyCommandHandler : IRequestHandler<FavoriteJourneyCommand, ApiResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public FavoriteJourneyCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(FavoriteJourneyCommand request, CancellationToken cancellationToken)
        {
            // Ensure user is authenticated
            if (!_currentUserService.IsAuthenticated)
            {
                return ApiResponse.CreateFailure("User must be authenticated to favorite a journey.");
            }

            // Check if the journey exists
            var journey = await _dbContext.Journeys
                .FirstOrDefaultAsync(j => j.Id == request.JourneyId && !j.IsDeleted, cancellationToken);

            if (journey == null)
            {
                return ApiResponse.CreateFailure("Journey not found.");
            }

            // Check if the favorite already exists (operation is idempotent)
            var existingFavorite = await _dbContext.JourneyFavorites
                .FirstOrDefaultAsync(f => 
                    f.JourneyId == request.JourneyId && 
                    f.UserId == _currentUserService.UserId, 
                    cancellationToken);

            if (existingFavorite != null)
            {
                // Already favorited, return success (idempotent operation)
                return ApiResponse.CreateSuccess("Journey is already in favorites.");
            }

            // Create new favorite entry
            var favorite = new JourneyFavorite
            {
                JourneyId = request.JourneyId,
                UserId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.JourneyFavorites.Add(favorite);

            // Create audit entry
            var auditLog = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "JourneyFavorited",
                Content = System.Text.Json.JsonSerializer.Serialize(new
                {
                    UserId = _currentUserService.UserId,
                    JourneyId = request.JourneyId,
                    Timestamp = DateTime.UtcNow
                }),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.OutboxMessages.Add(auditLog);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse.CreateSuccess("Journey added to favorites.");
        }
    }
} 