using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Enums;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Application.Features.Journeys.Commands.UpdateJourney
{
    public class UpdateJourneyCommand : IRequest<ApiResponse>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string StartLocation { get; set; }
        public DateTime StartTime { get; set; }
        public string ArrivalLocation { get; set; }
        public DateTime ArrivalTime { get; set; }
        public TransportType TransportType { get; set; }
        public decimal DistanceKm { get; set; }
        public string RouteDataUrl { get; set; }
        public bool IsPublic { get; set; }
    }

    public class UpdateJourneyCommandValidator : AbstractValidator<UpdateJourneyCommand>
    {
        public UpdateJourneyCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Journey Id is required");

            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(v => v.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

            RuleFor(v => v.StartLocation)
                .NotEmpty().WithMessage("Start location is required")
                .MaximumLength(100).WithMessage("Start location must not exceed 100 characters");

            RuleFor(v => v.StartTime)
                .NotEmpty().WithMessage("Start time is required");

            RuleFor(v => v.ArrivalLocation)
                .NotEmpty().WithMessage("Arrival location is required")
                .MaximumLength(100).WithMessage("Arrival location must not exceed 100 characters");

            RuleFor(v => v.ArrivalTime)
                .NotEmpty().WithMessage("Arrival time is required")
                .GreaterThan(v => v.StartTime).WithMessage("Arrival time must be after start time");

            RuleFor(v => v.DistanceKm)
                .GreaterThan(0).WithMessage("Distance must be greater than 0");

            RuleFor(v => v.RouteDataUrl)
                .MaximumLength(1000).WithMessage("Route data URL must not exceed 1000 characters");
        }
    }

    public class UpdateJourneyCommandHandler : IRequestHandler<UpdateJourneyCommand, ApiResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IJourneyNotificationService _notificationService;
        private readonly ILogger<UpdateJourneyCommandHandler> _logger;

        public UpdateJourneyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IEventPublisher eventPublisher,
            IJourneyNotificationService notificationService,
            ILogger<UpdateJourneyCommandHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _eventPublisher = eventPublisher;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<ApiResponse> Handle(UpdateJourneyCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            _logger.LogInformation("Handling UpdateJourneyCommand for journey {JourneyId} by user {UserId}", 
                request.Id, userId);
                
            var journey = await _context.Journeys
                .FirstOrDefaultAsync(j => j.Id == request.Id && !j.IsDeleted, cancellationToken);

            if (journey == null)
            {
                _logger.LogWarning("Journey not found: {JourneyId}", request.Id);
                throw new NotFoundException("Journey not found");
            }

            // Strict owner-only check as per specification
            if (journey.OwnerId != userId)
            {
                _logger.LogWarning("Unauthorized update attempt: Journey {JourneyId} owned by {OwnerId}, request by {UserId}",
                    request.Id, journey.OwnerId, userId);
                throw new ForbiddenAccessException("You do not have permission to update this journey. Only the owner can update a journey.");
            }

            // Update properties
            journey.Name = request.Name;
            journey.Description = request.Description;
            journey.StartLocation = request.StartLocation;
            journey.StartTime = request.StartTime;
            journey.ArrivalLocation = request.ArrivalLocation;
            journey.ArrivalTime = request.ArrivalTime;
            journey.TransportType = request.TransportType;
            journey.DistanceKm = request.DistanceKm;
            journey.RouteDataUrl = request.RouteDataUrl;
            journey.IsPublic = request.IsPublic;
            journey.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully updated journey {JourneyId}", request.Id);

            // Publish domain event
            await _eventPublisher.PublishAsync(new JourneyUpdatedEvent(journey, userId), cancellationToken);
            
            // Notify users who favorited this journey
            await _notificationService.NotifyJourneyUpdated(journey.Id);

            return ApiResponse.CreateSuccess("Journey updated successfully");
        }
    }
} 