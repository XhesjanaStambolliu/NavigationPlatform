using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;

namespace NavigationPlatform.Application.Features.Journeys.Queries.GetJourneys
{
    public class GetJourneysQuery : IRequest<ApiResponse<PaginatedList<JourneyDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetJourneysQueryValidator : AbstractValidator<GetJourneysQuery>
    {
        public GetJourneysQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(50).WithMessage("Page size must not exceed 50");
        }
    }

    public class GetJourneysQueryHandler : IRequestHandler<GetJourneysQuery, ApiResponse<PaginatedList<JourneyDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetJourneysQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<PaginatedList<JourneyDto>>> Handle(GetJourneysQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var isAuthenticated = _currentUserService.IsAuthenticated;

            // Build query
            var query = _context.Journeys
                .AsNoTracking()
                .Where(j => !j.IsDeleted);

            // Show only public journeys
            query = query.Where(j => j.IsPublic);

            // Order by most recent
            query = query.OrderByDescending(j => j.CreatedAt);

            // Get the user's favorites if authenticated
            HashSet<Guid> userFavorites = new HashSet<Guid>();
            if (isAuthenticated)
            {
                userFavorites = await _context.JourneyFavorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.JourneyId)
                    .ToHashSetAsync(cancellationToken);
            }

            // Apply pagination
            var journeysToReturn = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Project to DTOs with favorites information
            var journeyDtos = journeysToReturn.Select(j => new JourneyDto
            {
                Id = j.Id,
                Name = j.Name,
                Description = j.Description,
                OwnerId = j.OwnerId,
                StartLocation = j.StartLocation,
                StartTime = j.StartTime,
                ArrivalLocation = j.ArrivalLocation,
                ArrivalTime = j.ArrivalTime,
                TransportType = j.TransportType,
                DistanceKm = j.DistanceKm,
                IsPublic = j.IsPublic,
                RouteDataUrl = j.RouteDataUrl,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt,
                IsFavorite = userFavorites.Contains(j.Id)
            }).ToList();

            var paginatedList = new PaginatedList<JourneyDto>(
                journeyDtos,
                totalCount,
                request.Page,
                request.PageSize);

            return ApiResponse<PaginatedList<JourneyDto>>.CreateSuccess(paginatedList);
        }
    }
} 