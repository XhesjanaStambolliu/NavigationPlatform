using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MediatR;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Application.Features.Journeys
{
    public class DailyDistanceRewardConsumer : 
        INotificationHandler<JourneyCreatedEvent>,
        INotificationHandler<JourneyUpdatedEvent>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<DailyDistanceRewardConsumer> _logger;
        private const decimal DAILY_GOAL_KM = 20.00m;

        public DailyDistanceRewardConsumer(
            IApplicationDbContext dbContext,
            IEventPublisher eventPublisher,
            ILogger<DailyDistanceRewardConsumer> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task Handle(JourneyCreatedEvent notification, CancellationToken cancellationToken)
        {
            await ProcessJourneyEventAsync(notification.Journey, notification.JourneyId, notification.UserId, cancellationToken);
        }

        public async Task Handle(JourneyUpdatedEvent notification, CancellationToken cancellationToken)
        {
            await ProcessJourneyEventAsync(notification.Journey, notification.JourneyId, notification.UserId, cancellationToken);
        }

        private async Task ProcessJourneyEventAsync(Journey journey, Guid journeyId, Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                // If the journey is null, look it up from the database
                if (journey == null)
                {
                    journey = await _dbContext.Journeys
                        .FirstOrDefaultAsync(j => j.Id == journeyId && !j.IsDeleted, cancellationToken);
                    
                    if (journey == null)
                    {
                        _logger.LogWarning("Journey with ID {JourneyId} not found", journeyId);
                        return;
                    }
                }
                
                var journeyDate = journey.StartTime.Date;
                
                // Check if the user already has a badge for this day
                bool alreadyAwarded = await _dbContext.DailyDistanceBadges
                    .AnyAsync(b => b.UserId == userId && b.AwardDate.Date == journeyDate, cancellationToken);

                if (alreadyAwarded)
                {
                    return;
                }

                // Calculate total distance for the day
                var totalDistanceKm = await _dbContext.Journeys
                    .Where(j => j.OwnerId == userId && 
                           j.StartTime.Date == journeyDate &&
                           !j.IsDeleted)
                    .SumAsync(j => j.DistanceKm, cancellationToken);

                // Check if distance exceeds the goal and award badge if needed
                if (totalDistanceKm >= DAILY_GOAL_KM)
                {
                    // Update this journey as the one that triggered the goal achievement
                    journey.IsDailyGoalAchieved = true;
                    
                    // Create the badge record
                    var badge = new DailyDistanceBadge
                    {
                        UserId = userId,
                        JourneyId = journey.Id,
                        AwardDate = journeyDate,
                        TotalDistanceKm = totalDistanceKm
                    };
                    
                    _dbContext.DailyDistanceBadges.Add(badge);
                    
                    // Publish the domain event
                    var dailyGoalEvent = new DailyGoalAchievedEvent(
                        journey, 
                        userId, 
                        totalDistanceKm, 
                        journeyDate);
                    
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await _eventPublisher.PublishAsync(dailyGoalEvent, cancellationToken);
                    
                    _logger.LogInformation("Daily distance goal achieved for user {UserId} with {Distance} km", 
                        userId, totalDistanceKm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing daily distance reward for journey {JourneyId}", journeyId);
                throw;
            }
        }
    }
} 