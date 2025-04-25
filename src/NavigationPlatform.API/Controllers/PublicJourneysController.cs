using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Journeys.Queries.GetPublicJourney;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;

namespace NavigationPlatform.API.Controllers
{
    [ApiController]
    [Route("api/public/journeys")]
    public class PublicJourneysController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PublicJourneysController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get a journey by its public link token
        /// </summary>
        /// <param name="token">Public link token</param>
        /// <returns>Journey details if the token is valid</returns>
        [HttpGet("{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<JourneyDto>>> GetJourneyByToken(string token)
        {
            var query = new GetPublicJourneyQuery { Token = token };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
} 