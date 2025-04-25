using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Enums;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Application.Features.Journeys.Commands.CreateJourney
{
    public class CreateJourneyCommand : IRequest<ApiResponse<Guid>>
    {
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

    public class CreateJourneyCommandValidator : AbstractValidator<CreateJourneyCommand>
    {
        public CreateJourneyCommandValidator()
        {
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

    public class CreateJourneyCommandHandler : IRequestHandler<CreateJourneyCommand, ApiResponse<Guid>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventPublisher _eventPublisher;

        public CreateJourneyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IEventPublisher eventPublisher)
        {
            _context = context;
            _currentUserService = currentUserService;
            _eventPublisher = eventPublisher;
        }

        public async Task<ApiResponse<Guid>> Handle(CreateJourneyCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var journey = new Journey
            {
                Name = request.Name,
                Description = request.Description,
                OwnerId = userId,
                StartLocation = request.StartLocation,
                StartTime = request.StartTime,
                ArrivalLocation = request.ArrivalLocation,
                ArrivalTime = request.ArrivalTime,
                TransportType = request.TransportType,
                DistanceKm = request.DistanceKm,
                RouteDataUrl = request.RouteDataUrl,
                IsPublic = request.IsPublic,
                IsDeleted = false
            };

            _context.Journeys.Add(journey);
            await _context.SaveChangesAsync(cancellationToken);

            // Publish domain event
            await _eventPublisher.PublishAsync(new JourneyCreatedEvent(journey, userId), cancellationToken);

            return ApiResponse<Guid>.CreateSuccess(
                journey.Id,
                "Journey created successfully"
            );
        }
    }
} 