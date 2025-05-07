using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;

namespace NavigationPlatform.Application.Features.Journeys.Queries.GetFavorites
{
    public class GetFavoritesQuery : IRequest<ApiResponse<PaginatedList<JourneyDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetFavoritesQueryHandler : IRequestHandler<GetFavoritesQuery, ApiResponse<PaginatedList<JourneyDto>>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetFavoritesQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<PaginatedList<JourneyDto>>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
        {
            // Ensure user is authenticated
            if (!_currentUserService.IsAuthenticated)
            {
                return ApiResponse<PaginatedList<JourneyDto>>.CreateFailure("User must be authenticated to view favorites.");
            }

            // Normalize page parameters
            request.Page = request.Page < 1 ? 1 : request.Page;
            request.PageSize = request.PageSize switch
            {
                < 1 => 20,
                > 50 => 50,
                _ => request.PageSize
            };

            // Query favorite journeys
            var query = _dbContext.JourneyFavorites
                .Where(f => f.UserId == _currentUserService.UserId)
                .Join(
                    _dbContext.Journeys.Where(j => !j.IsDeleted),
                    favorite => favorite.JourneyId,
                    journey => journey.Id,
                    (favorite, journey) => journey
                )
                .OrderByDescending(j => j.CreatedAt)
                .AsQueryable();

            // Calculate total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Skip and take for pagination
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(j => new JourneyDto
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
                    IsFavorite = true // By definition, these are all favorites
                })
                .ToListAsync(cancellationToken);

            var paginatedResult = new PaginatedList<JourneyDto>(
                items,
                totalCount,
                request.Page,
                request.PageSize
            );

            return ApiResponse<PaginatedList<JourneyDto>>.CreateSuccess(paginatedResult);
        }
    }
} 