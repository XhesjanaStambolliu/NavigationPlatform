using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Admin.Commands.ChangeUserStatus;
using NavigationPlatform.Application.Features.Admin.Queries.GetFilteredJourneys;
using NavigationPlatform.Application.Features.Admin.Queries.GetMonthlyDistanceStatistics;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;
using NavigationPlatform.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace NavigationPlatform.API.Controllers
{
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.AdminPolicy)]
    [Route("admin")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public AdminController(
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Get a filtered list of journeys for admin
        /// </summary>
        /// <param name="userId">Filter by user ID</param>
        /// <param name="transportType">Filter by transport type</param>
        /// <param name="startDateFrom">Filter by start date (from)</param>
        /// <param name="startDateTo">Filter by start date (to)</param>
        /// <param name="arrivalDateFrom">Filter by arrival date (from)</param>
        /// <param name="arrivalDateTo">Filter by arrival date (to)</param>
        /// <param name="minDistance">Filter by minimum distance in kilometers</param>
        /// <param name="maxDistance">Filter by maximum distance in kilometers</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 50)</param>
        /// <param name="orderBy">Order by field</param>
        /// <param name="direction">Sort direction (asc or desc)</param>
        /// <returns>Filtered list of journeys</returns>
        [HttpGet("journeys")]
        public async Task<ActionResult<ApiResponse<PaginatedList<JourneyDto>>>> GetFilteredJourneys(
            [FromQuery] Guid? userId = null,
            [FromQuery] TransportType? transportType = null,
            [FromQuery] DateTime? startDateFrom = null,
            [FromQuery] DateTime? startDateTo = null,
            [FromQuery] DateTime? arrivalDateFrom = null,
            [FromQuery] DateTime? arrivalDateTo = null,
            [FromQuery] decimal? minDistance = null,
            [FromQuery] decimal? maxDistance = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string orderBy = "CreatedAt",
            [FromQuery] string direction = "desc")
        {
            var query = new AdminJourneyFilterQuery
            {
                UserId = userId,
                TransportType = transportType,
                StartDateFrom = startDateFrom,
                StartDateTo = startDateTo,
                ArrivalDateFrom = arrivalDateFrom,
                ArrivalDateTo = arrivalDateTo,
                MinDistance = minDistance,
                MaxDistance = maxDistance,
                Page = page,
                PageSize = pageSize,
                OrderBy = orderBy,
                Direction = direction
            };

            var result = await _mediator.Send(query);
            
            // Add total count to response headers
            Response.Headers.Add("X-Total-Count", result.Data.TotalCount.ToString());
            
            return Ok(result);
        }

        /// <summary>
        /// Get monthly distance statistics
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 50)</param>
        /// <param name="orderBy">Order by field (UserId or TotalDistanceKm)</param>
        /// <param name="direction">Sort direction (asc or desc)</param>
        /// <returns>List of monthly distance statistics</returns>
        [HttpGet("statistics/monthly-distance")]
        public async Task<ActionResult<ApiResponse<PaginatedList<MonthlyDistanceStatisticDto>>>> GetMonthlyDistanceStatistics(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string orderBy = "TotalDistanceKm",
            [FromQuery] string direction = "desc")
        {
            var query = new GetMonthlyDistanceStatisticsQuery
            {
                Page = page,
                PageSize = pageSize,
                OrderBy = orderBy,
                Direction = direction
            };

            var result = await _mediator.Send(query);
            
            // Add total count to response headers
            Response.Headers.Add("X-Total-Count", result.Data.TotalCount.ToString());
            
            return Ok(result);
        }
        
        /// <summary>
        /// Change a user's status
        /// </summary>
        /// <param name="id">The user ID</param>
        /// <param name="request">The status change request</param>
        /// <returns>Success or failure response</returns>
        [HttpPatch("users/{id}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> ChangeUserStatus(
            [FromRoute] Guid id,
            [FromBody] ChangeUserStatusRequest request)
        {
            // Use the current authenticated admin user's ID for proper auditing
            var command = new ChangeUserStatusCommand
            {
                UserId = id,
                Status = request.Status,
                AdminId = _currentUserService.UserId,
                Reason = request.Reason
            };
            
            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                
                return BadRequest(result);
            }
            
            return Ok(result);
        }
    }
} 