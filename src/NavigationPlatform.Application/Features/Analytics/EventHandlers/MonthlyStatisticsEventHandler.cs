using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MediatR;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Application.Features.Analytics.EventHandlers
{
    public class MonthlyStatisticsEventHandler : 
        INotificationHandler<JourneyCreatedEvent>,
        INotificationHandler<JourneyUpdatedEvent>,
        INotificationHandler<JourneyDeletedEvent>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<MonthlyStatisticsEventHandler> _logger;

        public MonthlyStatisticsEventHandler(
            IApplicationDbContext dbContext,
            ILogger<MonthlyStatisticsEventHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Handle(JourneyCreatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.Journey == null)
            {
                var journey = await _dbContext.Journeys
                    .FirstOrDefaultAsync(j => j.Id == notification.JourneyId && !j.IsDeleted, cancellationToken);
                
                if (journey == null)
                {
                    _logger.LogWarning("Journey with ID {JourneyId} not found", notification.JourneyId);
                    return;
                }
                
                await UpdateMonthlyStatistics(journey, cancellationToken);
            }
            else
            {
                await UpdateMonthlyStatistics(notification.Journey, cancellationToken);
            }
        }

        public async Task Handle(JourneyUpdatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.Journey == null)
            {
                var journey = await _dbContext.Journeys
                    .FirstOrDefaultAsync(j => j.Id == notification.JourneyId && !j.IsDeleted, cancellationToken);
                
                if (journey == null)
                {
                    _logger.LogWarning("Journey with ID {JourneyId} not found", notification.JourneyId);
                    return;
                }
                
                await UpdateMonthlyStatistics(journey, cancellationToken);
            }
            else
            {
                await UpdateMonthlyStatistics(notification.Journey, cancellationToken);
            }
        }

        public async Task Handle(JourneyDeletedEvent notification, CancellationToken cancellationToken)
        {
            // For deleted journeys, we need to fetch the journey first since the event only contains the ID
            var journey = await _dbContext.Journeys
                .FirstOrDefaultAsync(j => j.Id == notification.JourneyId, cancellationToken);
                
            if (journey != null)
            {
                await UpdateMonthlyStatistics(journey, cancellationToken);
            }
        }

        private async Task UpdateMonthlyStatistics(Journey journey, CancellationToken cancellationToken)
        {
            try
            {
                var userId = journey.OwnerId;
                var year = journey.StartTime.Year;
                var month = journey.StartTime.Month;

                // Find or create the monthly statistic record
                var monthlyStats = await _dbContext.MonthlyUserDistances
                    .FirstOrDefaultAsync(m => 
                        m.UserId == userId && 
                        m.Year == year && 
                        m.Month == month, 
                        cancellationToken);

                if (monthlyStats == null)
                {
                    // Create new record if it doesn't exist
                    monthlyStats = new MonthlyUserDistance
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Year = year,
                        Month = month,
                        TotalDistanceKm = 0,
                        JourneyCount = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.MonthlyUserDistances.Add(monthlyStats);
                }

                // Calculate aggregated statistics for the month
                var journeysInMonth = await _dbContext.Journeys
                    .Where(j => 
                        j.OwnerId == userId && 
                        j.StartTime.Year == year && 
                        j.StartTime.Month == month &&
                        !j.IsDeleted)
                    .ToListAsync(cancellationToken);

                var totalDistance = journeysInMonth.Sum(j => (double)j.DistanceKm);
                var journeyCount = journeysInMonth.Count;

                // Update the statistics
                monthlyStats.TotalDistanceKm = totalDistance;
                monthlyStats.JourneyCount = journeyCount;
                monthlyStats.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating monthly statistics for journey {JourneyId}", journey.Id);
                throw;
            }
        }
    }
} 