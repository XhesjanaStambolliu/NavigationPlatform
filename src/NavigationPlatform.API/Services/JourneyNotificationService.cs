using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.API.Hubs;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.API.Services
{
    public class JourneyNotificationService : IJourneyNotificationService
    {
        private readonly IHubContext<JourneyHub> _hubContext;
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<JourneyNotificationService> _logger;

        public JourneyNotificationService(
            IHubContext<JourneyHub> hubContext,
            IApplicationDbContext dbContext,
            ILogger<JourneyNotificationService> logger)
        {
            _hubContext = hubContext;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task NotifyJourneyUpdated(Guid journeyId)
        {
            await NotifyFavoriteUsers(journeyId, "JourneyUpdated");
        }

        public async Task NotifyJourneyDeleted(Guid journeyId)
        {
            await NotifyFavoriteUsers(journeyId, "JourneyDeleted");
        }

        public async Task SendFallbackNotificationAsync(Guid userId, Guid journeyId, string messageType)
        {
            // In a real implementation, this would queue an email notification
            // For now, we'll just log it and create an outbox message
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "FallbackNotification",
                Content = System.Text.Json.JsonSerializer.Serialize(new
                {
                    UserId = userId,
                    JourneyId = journeyId,
                    MessageType = messageType,
                    Timestamp = DateTime.UtcNow
                }),
                CreatedAt = DateTime.UtcNow
            };
            
            _dbContext.OutboxMessages.Add(message);
            await _dbContext.SaveChangesAsync(new System.Threading.CancellationToken());
        }

        private async Task NotifyFavoriteUsers(Guid journeyId, string methodName)
        {
            try
            {
                // Get all users who favorited this journey
                var userIds = await _dbContext.JourneyFavorites
                    .Where(f => f.JourneyId == journeyId)
                    .Select(f => f.UserId)
                    .ToListAsync();

                if (!userIds.Any())
                {
                    return;
                }

                // Get journey details to send in the notification
                var journey = await _dbContext.Journeys
                    .Where(j => j.Id == journeyId)
                    .Select(j => new
                    {
                        j.Id,
                        j.Name,
                        j.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (journey == null)
                {
                    _logger.LogWarning("Attempted to notify about non-existent journey {JourneyId}", journeyId);
                    return;
                }

                // Create notification data
                var notificationData = new
                {
                    journey.Id,
                    journey.Name,
                    Timestamp = DateTime.UtcNow,
                    EventType = methodName
                };

                var connectedUsers = new List<Guid>();
                var offlineUsers = new List<Guid>();

                // For each user who favorited the journey, check if they're connected
                foreach (var userId in userIds)
                {
                    // In a real implementation, you would check if the user is connected to SignalR
                    // For this example, we'll just try to send to all users
                    try
                    {
                        // Send the notification to the user's group
                        await _hubContext.Clients.Group(userId.ToString())
                            .SendAsync(methodName, notificationData);
                        
                        connectedUsers.Add(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
                        offlineUsers.Add(userId);
                    }
                }

                // For offline users, queue fallback notifications
                foreach (var userId in offlineUsers)
                {
                    await SendFallbackNotificationAsync(userId, journeyId, methodName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying users about journey {JourneyId}", journeyId);
            }
        }
    }
} 