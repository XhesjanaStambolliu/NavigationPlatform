using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Application.Features.Journeys.Commands.UnfavoriteJourney
{
    public class UnfavoriteJourneyCommand : IRequest<ApiResponse>
    {
        public Guid JourneyId { get; set; }
    }

    public class UnfavoriteJourneyCommandValidator : AbstractValidator<UnfavoriteJourneyCommand>
    {
        public UnfavoriteJourneyCommandValidator()
        {
            RuleFor(x => x.JourneyId)
                .NotEmpty().WithMessage("Journey ID is required.");
        }
    }

    public class UnfavoriteJourneyCommandHandler : IRequestHandler<UnfavoriteJourneyCommand, ApiResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public UnfavoriteJourneyCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse> Handle(UnfavoriteJourneyCommand request, CancellationToken cancellationToken)
        {
            // Ensure user is authenticated
            if (!_currentUserService.IsAuthenticated)
            {
                return ApiResponse.CreateFailure("User must be authenticated to unfavorite a journey.");
            }

            // Find the favorite entry
            var favorite = await _dbContext.JourneyFavorites
                .FirstOrDefaultAsync(f => 
                    f.JourneyId == request.JourneyId && 
                    f.UserId == _currentUserService.UserId, 
                    cancellationToken);

            // If not found, return success (operation is idempotent)
            if (favorite == null)
            {
                return ApiResponse.CreateSuccess("Journey was not in favorites.");
            }

            // Remove the favorite
            _dbContext.JourneyFavorites.Remove(favorite);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse.CreateSuccess("Journey removed from favorites.");
        }
    }
} 