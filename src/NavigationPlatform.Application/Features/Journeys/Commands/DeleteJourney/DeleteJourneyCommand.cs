using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Exceptions;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Application.Features.Journeys.Commands.DeleteJourney
{
    public class DeleteJourneyCommand : IRequest<ApiResponse>
    {
        public Guid Id { get; set; }
    }

    public class DeleteJourneyCommandValidator : AbstractValidator<DeleteJourneyCommand>
    {
        public DeleteJourneyCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Journey Id is required");
        }
    }

    public class DeleteJourneyCommandHandler : IRequestHandler<DeleteJourneyCommand, ApiResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IJourneyNotificationService _notificationService;

        public DeleteJourneyCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IEventPublisher eventPublisher,
            IJourneyNotificationService notificationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _eventPublisher = eventPublisher;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse> Handle(DeleteJourneyCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var journey = await _context.Journeys
                .FirstOrDefaultAsync(j => j.Id == request.Id && !j.IsDeleted, cancellationToken);

            if (journey == null)
            {
                throw new NotFoundException("Journey not found");
            }

            if (journey.OwnerId != userId)
            {
                throw new ForbiddenAccessException("You do not have permission to delete this journey");
            }

            // Soft delete the journey
            journey.IsDeleted = true;
            journey.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Publish domain event
            await _eventPublisher.PublishAsync(new JourneyDeletedEvent(journey.Id, userId), cancellationToken);
            
            // Notify users who favorited this journey
            await _notificationService.NotifyJourneyDeleted(journey.Id);

            return ApiResponse.CreateSuccess("Journey deleted successfully");
        }
    }
} 