using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;

namespace NavigationPlatform.Application.Features.Admin.Queries.GetMonthlyDistanceStatistics
{
    public class GetMonthlyDistanceStatisticsQuery : IRequest<ApiResponse<PaginatedList<MonthlyDistanceStatisticDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string OrderBy { get; set; } = "TotalDistanceKm";
        public string Direction { get; set; } = "desc";
    }
    
    public class MonthlyDistanceStatisticDto
    {
        public Guid UserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public double TotalDistanceKm { get; set; }
    }
    
    public class GetMonthlyDistanceStatisticsQueryValidator : AbstractValidator<GetMonthlyDistanceStatisticsQuery>
    {
        public GetMonthlyDistanceStatisticsQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0");
                
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(50).WithMessage("Page size must not exceed 50");
                
            RuleFor(x => x.OrderBy)
                .Must(ValidOrderByField)
                .WithMessage("Invalid order by field. Valid fields are: UserId or TotalDistanceKm");
                
            RuleFor(x => x.Direction)
                .Must(x => x == null || x.ToLower() == "asc" || x.ToLower() == "desc")
                .WithMessage("Direction must be 'asc' or 'desc'");
        }
        
        private bool ValidOrderByField(string orderBy)
        {
            if (string.IsNullOrEmpty(orderBy))
                return true;
                
            var validFields = new[]
            {
                "userid", "totaldistancekm"
            };
            
            return validFields.Contains(orderBy.ToLower());
        }
    }
    
    public class GetMonthlyDistanceStatisticsQueryHandler 
        : IRequestHandler<GetMonthlyDistanceStatisticsQuery, ApiResponse<PaginatedList<MonthlyDistanceStatisticDto>>>
    {
        private readonly IApplicationDbContext _dbContext;
        
        public GetMonthlyDistanceStatisticsQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public async Task<ApiResponse<PaginatedList<MonthlyDistanceStatisticDto>>> Handle(
            GetMonthlyDistanceStatisticsQuery request, 
            CancellationToken cancellationToken)
        {
            var query = _dbContext.MonthlyUserDistances.AsQueryable();
            
            // Apply sorting
            query = ApplySorting(query, request.OrderBy, request.Direction);
            
            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);
            
            // Apply pagination and projection
            var statistics = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => new MonthlyDistanceStatisticDto
                {
                    UserId = m.UserId,
                    Year = m.Year,
                    Month = m.Month,
                    TotalDistanceKm = m.TotalDistanceKm
                })
                .ToListAsync(cancellationToken);
                
            var paginatedList = new PaginatedList<MonthlyDistanceStatisticDto>(
                statistics,
                totalCount,
                request.Page,
                request.PageSize);
                
            return ApiResponse<PaginatedList<MonthlyDistanceStatisticDto>>.CreateSuccess(paginatedList);
        }
        
        private IQueryable<Domain.Entities.MonthlyUserDistance> ApplySorting(
            IQueryable<Domain.Entities.MonthlyUserDistance> query, 
            string orderBy, 
            string direction)
        {
            var isAscending = string.IsNullOrEmpty(direction) || direction.ToLower() == "asc";
            
            // Default sort by TotalDistanceKm
            if (string.IsNullOrEmpty(orderBy))
                return isAscending 
                    ? query.OrderBy(m => m.TotalDistanceKm) 
                    : query.OrderByDescending(m => m.TotalDistanceKm);
                    
            // Apply specific sort
            switch (orderBy.ToLower())
            {
                case "userid":
                    return isAscending 
                        ? query.OrderBy(m => m.UserId) 
                        : query.OrderByDescending(m => m.UserId);
                case "totaldistancekm":
                default:
                    return isAscending 
                        ? query.OrderBy(m => m.TotalDistanceKm) 
                        : query.OrderByDescending(m => m.TotalDistanceKm);
            }
        }
    }
} 