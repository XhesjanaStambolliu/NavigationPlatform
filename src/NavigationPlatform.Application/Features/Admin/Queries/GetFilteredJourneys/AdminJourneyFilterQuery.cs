using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Application.Features.Admin.Queries.GetFilteredJourneys
{
    public class AdminJourneyFilterQuery : IRequest<ApiResponse<PaginatedList<JourneyDto>>>
    {
        // Filtering
        public Guid? UserId { get; set; }
        public TransportType? TransportType { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? ArrivalDateFrom { get; set; }
        public DateTime? ArrivalDateTo { get; set; }
        public decimal? MinDistance { get; set; }
        public decimal? MaxDistance { get; set; }
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // Sorting
        public string OrderBy { get; set; } = "CreatedAt";
        public string Direction { get; set; } = "desc";
    }
    
    public class AdminJourneyFilterQueryValidator : AbstractValidator<AdminJourneyFilterQuery>
    {
        public AdminJourneyFilterQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0");
                
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(50).WithMessage("Page size must not exceed 50");
                
            RuleFor(x => x.MinDistance)
                .GreaterThanOrEqualTo(0).When(x => x.MinDistance.HasValue)
                .WithMessage("Minimum distance must be greater than or equal to 0");
                
            RuleFor(x => x.MaxDistance)
                .GreaterThan(0).When(x => x.MaxDistance.HasValue)
                .WithMessage("Maximum distance must be greater than 0");
                
            RuleFor(x => x.StartDateFrom)
                .LessThanOrEqualTo(x => x.StartDateTo)
                .When(x => x.StartDateFrom.HasValue && x.StartDateTo.HasValue)
                .WithMessage("Start date from must be less than or equal to start date to");
                
            RuleFor(x => x.ArrivalDateFrom)
                .LessThanOrEqualTo(x => x.ArrivalDateTo)
                .When(x => x.ArrivalDateFrom.HasValue && x.ArrivalDateTo.HasValue)
                .WithMessage("Arrival date from must be less than or equal to arrival date to");
                
            RuleFor(x => x.Direction)
                .Must(x => x == null || x.ToLower() == "asc" || x.ToLower() == "desc")
                .WithMessage("Direction must be 'asc' or 'desc'");
                
            RuleFor(x => x.OrderBy)
                .Must(ValidOrderByField)
                .WithMessage("Invalid order by field. Valid fields are: Id, Name, OwnerId, StartTime, ArrivalTime, TransportType, DistanceKm, CreatedAt");
        }
        
        private bool ValidOrderByField(string orderBy)
        {
            if (string.IsNullOrEmpty(orderBy))
                return true;
                
            var validFields = new[]
            {
                "id", "name", "ownerid", "starttime", "arrivaltime", 
                "transporttype", "distancekm", "createdat"
            };
            
            return validFields.Contains(orderBy.ToLower());
        }
    }
    
    public class AdminJourneyFilterQueryHandler : IRequestHandler<AdminJourneyFilterQuery, ApiResponse<PaginatedList<JourneyDto>>>
    {
        private readonly IApplicationDbContext _context;
        
        public AdminJourneyFilterQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<ApiResponse<PaginatedList<JourneyDto>>> Handle(AdminJourneyFilterQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Journeys
                .Include(j => j.Owner)
                .Where(j => !j.IsDeleted)
                .AsQueryable();
                
            // Apply filters
            if (request.UserId.HasValue)
                query = query.Where(j => j.OwnerId == request.UserId);
                
            if (request.TransportType.HasValue)
                query = query.Where(j => j.TransportType == request.TransportType);
                
            if (request.StartDateFrom.HasValue)
                query = query.Where(j => j.StartTime >= request.StartDateFrom.Value);
                
            if (request.StartDateTo.HasValue)
                query = query.Where(j => j.StartTime <= request.StartDateTo.Value);
                
            if (request.ArrivalDateFrom.HasValue)
                query = query.Where(j => j.ArrivalTime >= request.ArrivalDateFrom.Value);
                
            if (request.ArrivalDateTo.HasValue)
                query = query.Where(j => j.ArrivalTime <= request.ArrivalDateTo.Value);
                
            if (request.MinDistance.HasValue)
                query = query.Where(j => j.DistanceKm >= request.MinDistance.Value);
                
            if (request.MaxDistance.HasValue)
                query = query.Where(j => j.DistanceKm <= request.MaxDistance.Value);
                
            // Apply sorting
            query = ApplySorting(query, request.OrderBy, request.Direction);
            
            // Get total count for pagination header
            var totalCount = await query.CountAsync(cancellationToken);
            
            // Apply pagination
            var journeys = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
                
            // Map to DTOs
            var journeyDtos = journeys.Select(j => new JourneyDto
            {
                Id = j.Id,
                Name = j.Name,
                Description = j.Description,
                OwnerId = j.OwnerId,
                OwnerName = j.Owner?.FullName ?? "Unknown",
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
                IsFavorite = false // Admin view doesn't need favorites
            }).ToList();
            
            var paginatedList = new PaginatedList<JourneyDto>(
                journeyDtos,
                totalCount,
                request.Page,
                request.PageSize);
                
            return ApiResponse<PaginatedList<JourneyDto>>.CreateSuccess(paginatedList);
        }
        
        private IQueryable<Domain.Entities.Journey> ApplySorting(
            IQueryable<Domain.Entities.Journey> query, 
            string orderBy, 
            string direction)
        {
            var isAscending = string.IsNullOrEmpty(direction) || direction.ToLower() == "asc";
            
            // Default sort
            if (string.IsNullOrEmpty(orderBy))
                return isAscending 
                    ? query.OrderBy(j => j.CreatedAt) 
                    : query.OrderByDescending(j => j.CreatedAt);
                    
            // Apply specific sort
            switch (orderBy.ToLower())
            {
                case "id":
                    return isAscending 
                        ? query.OrderBy(j => j.Id) 
                        : query.OrderByDescending(j => j.Id);
                case "name":
                    return isAscending 
                        ? query.OrderBy(j => j.Name) 
                        : query.OrderByDescending(j => j.Name);
                case "ownerid":
                    return isAscending 
                        ? query.OrderBy(j => j.OwnerId) 
                        : query.OrderByDescending(j => j.OwnerId);
                case "starttime":
                    return isAscending 
                        ? query.OrderBy(j => j.StartTime) 
                        : query.OrderByDescending(j => j.StartTime);
                case "arrivaltime":
                    return isAscending 
                        ? query.OrderBy(j => j.ArrivalTime) 
                        : query.OrderByDescending(j => j.ArrivalTime);
                case "transporttype":
                    return isAscending 
                        ? query.OrderBy(j => j.TransportType) 
                        : query.OrderByDescending(j => j.TransportType);
                case "distancekm":
                    return isAscending 
                        ? query.OrderBy(j => j.DistanceKm) 
                        : query.OrderByDescending(j => j.DistanceKm);
                case "createdat":
                default:
                    return isAscending 
                        ? query.OrderBy(j => j.CreatedAt) 
                        : query.OrderByDescending(j => j.CreatedAt);
            }
        }
    }
} 