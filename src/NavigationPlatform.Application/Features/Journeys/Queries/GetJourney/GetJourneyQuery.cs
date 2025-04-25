using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;

namespace NavigationPlatform.Application.Features.Journeys.Queries.GetJourney
{
    public class GetJourneyQuery : IRequest<ApiResponse<JourneyDto>>
    {
        public Guid Id { get; set; }
    }

    public class GetJourneyQueryValidator : AbstractValidator<GetJourneyQuery>
    {
        public GetJourneyQueryValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Journey Id is required");
        }
    }

    public class GetJourneyQueryHandler : IRequestHandler<GetJourneyQuery, ApiResponse<JourneyDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetJourneyQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<JourneyDto>> Handle(GetJourneyQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var journey = await _context.Journeys
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == request.Id && !j.IsDeleted, cancellationToken);

            if (journey == null)
            {
                throw new NotFoundException("Journey not found");
            }

            // Check if user has access (owner, or journey is public, or shared with user)
            bool hasAccess = journey.OwnerId == userId || journey.IsPublic;
            
            if (!hasAccess)
            {
                // Check if shared with user
                hasAccess = await _context.JourneyShares
                    .AnyAsync(s => s.JourneyId == journey.Id && s.UserId == userId, cancellationToken);
                
                if (!hasAccess)
                {
                    throw new ForbiddenAccessException("You do not have permission to view this journey");
                }
            }

            // Check if journey is favorited by current user
            bool isFavorite = false;
            if (_currentUserService.IsAuthenticated)
            {
                isFavorite = await _context.JourneyFavorites
                    .AnyAsync(f => f.JourneyId == journey.Id && f.UserId == userId, cancellationToken);
            }

            var journeyDto = new JourneyDto
            {
                Id = journey.Id,
                Name = journey.Name,
                Description = journey.Description,
                OwnerId = journey.OwnerId,
                StartLocation = journey.StartLocation,
                StartTime = journey.StartTime,
                ArrivalLocation = journey.ArrivalLocation,
                ArrivalTime = journey.ArrivalTime,
                TransportType = journey.TransportType,
                DistanceKm = journey.DistanceKm,
                AverageSpeedKmh = journey.AverageSpeedKmh,
                IsPublic = journey.IsPublic,
                RouteDataUrl = journey.RouteDataUrl,
                CreatedAt = journey.CreatedAt,
                UpdatedAt = journey.UpdatedAt,
                IsFavorite = isFavorite
            };

            return ApiResponse<JourneyDto>.CreateSuccess(journeyDto);
        }
    }
} 