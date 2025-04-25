using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Journeys.Commands.CreateJourney;
using NavigationPlatform.Application.Features.Journeys.Commands.DeleteJourney;
using NavigationPlatform.Application.Features.Journeys.Commands.FavoriteJourney;
using NavigationPlatform.Application.Features.Journeys.Commands.GeneratePublicLink;
using NavigationPlatform.Application.Features.Journeys.Commands.RevokePublicLink;
using NavigationPlatform.Application.Features.Journeys.Commands.ShareJourney;
using NavigationPlatform.Application.Features.Journeys.Commands.UnfavoriteJourney;
using NavigationPlatform.Application.Features.Journeys.Commands.UpdateJourney;
using NavigationPlatform.Application.Features.Journeys.Queries.GetFavorites;
using NavigationPlatform.Application.Features.Journeys.Queries.GetJourney;
using NavigationPlatform.Application.Features.Journeys.Queries.GetJourneys;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Infrastructure.Persistence;

namespace NavigationPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JourneysController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthorizationService _authorizationService;
        private readonly AppDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<JourneysController> _logger;

        public JourneysController(
            IMediator mediator, 
            IAuthorizationService authorizationService,
            AppDbContext dbContext,
            ICurrentUserService currentUserService,
            ILogger<JourneysController> logger)
        {
            _mediator = mediator;
            _authorizationService = authorizationService;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Get a list of journeys with pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 50)</param>
        /// <returns>Paged list of journeys</returns>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PaginatedList<JourneyDto>>>> GetJourneys(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetJourneysQuery
            {
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get a journey by ID
        /// </summary>
        /// <param name="id">Journey ID</param>
        /// <returns>Journey details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<JourneyDto>>> GetJourney(Guid id)
        {
            var journey = await _dbContext.Journeys.FindAsync(id);
            if (journey == null)
            {
                return NotFound(ApiResponse.CreateFailure($"Journey with ID {id} not found"));
            }

            var authResult = await _authorizationService.AuthorizeAsync(User, journey, AuthorizationPolicies.JourneyOwnerOrSharedPolicy);
            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            var query = new GetJourneyQuery { Id = id };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Create a new journey
        /// </summary>
        /// <param name="command">Journey data</param>
        /// <returns>Created journey ID</returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateJourney(CreateJourneyCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetJourney), new { id = result.Data }, result);
        }

        /// <summary>
        /// Update an existing journey
        /// </summary>
        /// <param name="id">Journey ID</param>
        /// <param name="command">Updated journey data</param>
        /// <returns>Success response</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateJourney(Guid id, UpdateJourneyCommand command)
        {
            if (id != command.Id)
            {
                _logger.LogWarning("ID mismatch in UpdateJourney: URL ID: {UrlId}, Body ID: {BodyId}", 
                    id, command.Id);
                return BadRequest(ApiResponse.CreateFailure("ID in the URL does not match the ID in the request body"));
            }

            var journey = await _dbContext.Journeys.FindAsync(id);
            if (journey == null)
            {
                _logger.LogWarning("Journey not found: {JourneyId}", id);
                return NotFound(ApiResponse.CreateFailure($"Journey with ID {id} not found"));
            }

            var currentUserId = _currentUserService.UserId;
            _logger.LogInformation("UpdateJourney: Journey owner: {OwnerId}, Current user: {UserId}", 
                journey.OwnerId, currentUserId);

            // Direct ownership check for clarity and debugging
            if (journey.OwnerId != currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to update journey {JourneyId} owned by {OwnerId}",
                    currentUserId, id, journey.OwnerId);
                return Forbid();
            }

            // Still use the authorization service for audit and consistency
            var authResult = await _authorizationService.AuthorizeAsync(User, journey, AuthorizationPolicies.JourneyOwnerOrSharedPolicy);
            if (!authResult.Succeeded)
            {
                _logger.LogWarning("Authorization failed for user {UserId} to update journey {JourneyId}",
                    currentUserId, id);
                return Forbid();
            }

            _logger.LogInformation("Updating journey {JourneyId} for user {UserId}", id, currentUserId);
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Delete a journey
        /// </summary>
        /// <param name="id">Journey ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteJourney(Guid id)
        {
            var command = new DeleteJourneyCommand { Id = id };
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Share a journey with specific users
        /// </summary>
        /// <param name="id">Journey ID</param>
        /// <param name="command">Share details including user IDs to share with</param>
        /// <returns>Success response</returns>
        [HttpPost("{id}/share")]
        [Authorize]
        public async Task<ActionResult> ShareJourney(Guid id, ShareJourneyCommand command)
        {
            if (id != command.JourneyId)
            {
                return BadRequest(ApiResponse.CreateFailure("ID in the URL does not match the ID in the request body"));
            }

            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Generate a public link for a journey
        /// </summary>
        /// <param name="id">Journey ID</param>
        /// <returns>The generated public link token</returns>
        [HttpPost("{id}/public-link")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> GeneratePublicLink(Guid id)
        {
            var command = new GeneratePublicLinkCommand { JourneyId = id };
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Revoke a public link for a journey
        /// </summary>
        /// <param name="id">Journey ID</param>
        /// <returns>Success response</returns>
        [HttpPost("{id}/revoke-link")]
        [Authorize]
        public async Task<ActionResult> RevokePublicLink(Guid id)
        {
            var command = new RevokePublicLinkCommand { JourneyId = id };
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Add a journey to favorites
        /// </summary>
        /// <param name="id">Journey ID</param>
        /// <returns>Success response</returns>
        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> FavoriteJourney(Guid id)
        {
            var command = new FavoriteJourneyCommand { JourneyId = id };
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Remove a journey from favorites
        /// </summary>
        /// <param name="id">Journey ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> UnfavoriteJourney(Guid id)
        {
            var command = new UnfavoriteJourneyCommand { JourneyId = id };
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Get the current user's favorite journeys
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 50)</param>
        /// <returns>Paged list of favorite journeys</returns>
        [HttpGet("favorites")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PaginatedList<JourneyDto>>>> GetFavorites(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetFavoritesQuery
            {
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
} 