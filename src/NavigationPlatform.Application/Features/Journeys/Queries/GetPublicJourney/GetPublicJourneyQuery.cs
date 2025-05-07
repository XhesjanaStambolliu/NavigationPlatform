using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;

namespace NavigationPlatform.Application.Features.Journeys.Queries.GetPublicJourney
{
    public class GetPublicJourneyQuery : IRequest<ApiResponse<JourneyDto>>
    {
        public string Token { get; set; }
    }

    public class GetPublicJourneyQueryValidator : AbstractValidator<GetPublicJourneyQuery>
    {
        public GetPublicJourneyQueryValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required");
        }
    }

    public class GetPublicJourneyQueryHandler : IRequestHandler<GetPublicJourneyQuery, ApiResponse<JourneyDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPublicJourneyQueryHandler> _logger;

        public GetPublicJourneyQueryHandler(
            IApplicationDbContext context,
            IMapper mapper,
            ILogger<GetPublicJourneyQueryHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<JourneyDto>> Handle(GetPublicJourneyQuery request, CancellationToken cancellationToken)
        {
            // Find the public link by token
            var publicLink = await _context.PublicLinks
                .Include(pl => pl.Journey)
                .FirstOrDefaultAsync(pl => pl.Token == request.Token, cancellationToken);

            if (publicLink == null)
            {
                throw new NotFoundException("Public link not found");
            }

            if (publicLink.IsDisabled)
            {
                throw new GoneException("This public link has been revoked");
            }

            if (publicLink.ExpiresAt.HasValue && publicLink.ExpiresAt.Value < DateTime.UtcNow)
            {
                throw new GoneException("This public link has expired");
            }

            if (publicLink.Journey.IsDeleted)
            {
                throw new NotFoundException("Journey not found");
            }

            // Increment access count
            publicLink.AccessCount++;
            await _context.SaveChangesAsync(cancellationToken);

            // Map journey to DTO
            var journeyDto = _mapper.Map<JourneyDto>(publicLink.Journey);

            return ApiResponse<JourneyDto>.CreateSuccess(
                journeyDto,
                "Journey retrieved successfully"
            );
        }
    }
} 