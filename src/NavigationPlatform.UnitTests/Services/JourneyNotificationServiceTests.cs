using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NavigationPlatform.API.Hubs;
using NavigationPlatform.API.Services;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Infrastructure.Persistence;
using Xunit;

namespace NavigationPlatform.UnitTests.Services
{
    public class JourneyNotificationServiceTests
    {
        private readonly Mock<IHubContext<JourneyHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IHubClients> _mockHubClients;
        private readonly Mock<ILogger<JourneyNotificationService>> _mockLogger;
        private readonly Guid _journeyId = Guid.NewGuid();
        private readonly Guid _userId1 = Guid.NewGuid();
        private readonly Guid _userId2 = Guid.NewGuid();

        public JourneyNotificationServiceTests()
        {
            _mockClientProxy = new Mock<IClientProxy>();
            _mockHubClients = new Mock<IHubClients>();
            _mockHubContext = new Mock<IHubContext<JourneyHub>>();
            _mockLogger = new Mock<ILogger<JourneyNotificationService>>();

            _mockHubClients
                .Setup(clients => clients.Group(It.IsAny<string>()))
                .Returns(_mockClientProxy.Object);

            _mockHubContext
                .Setup(context => context.Clients)
                .Returns(_mockHubClients.Object);
        }

        [Fact]
        public async Task NotifyJourneyUpdated_ShouldNotifyOnlyFavoriteUsers()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"NotificationTest_{Guid.NewGuid()}")
                .Options;

            // Seed the database with test data
            using (var context = new AppDbContext(options))
            {
                // Add a journey
                context.Journeys.Add(new Journey
                {
                    Id = _journeyId,
                    Name = "Test Journey",
                    Description = "Test journey description",
                    StartLocation = "Start",
                    ArrivalLocation = "Arrival",
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    ArrivalTime = DateTime.UtcNow.AddHours(-1),
                    RouteDataUrl = "https://example.com/route-data",
                    IsPublic = true,
                    IsDeleted = false
                });

                // Add users who favorited the journey
                context.JourneyFavorites.AddRange(
                    new JourneyFavorite { UserId = _userId1, JourneyId = _journeyId },
                    new JourneyFavorite { UserId = _userId2, JourneyId = _journeyId }
                );

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new AppDbContext(options))
            {
                var service = new JourneyNotificationService(_mockHubContext.Object, context, _mockLogger.Object);
                await service.NotifyJourneyUpdated(_journeyId);
            }

            // Assert
            // Verify both user groups were called with the message
            _mockHubClients.Verify(
                clients => clients.Group(_userId1.ToString()),
                Times.Once);

            _mockHubClients.Verify(
                clients => clients.Group(_userId2.ToString()),
                Times.Once);

            // Verify the JourneyUpdated method was called on the client proxies
            _mockClientProxy.Verify(
                proxy => proxy.SendCoreAsync(
                    "JourneyUpdated",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task NotifyJourneyDeleted_ShouldNotifyOnlyFavoriteUsers()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"NotificationTest_{Guid.NewGuid()}")
                .Options;

            // Seed the database with test data
            using (var context = new AppDbContext(options))
            {
                // Add a journey
                context.Journeys.Add(new Journey
                {
                    Id = _journeyId,
                    Name = "Test Journey",
                    Description = "Test journey description",
                    StartLocation = "Start",
                    ArrivalLocation = "Arrival",
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    ArrivalTime = DateTime.UtcNow.AddHours(-1),
                    RouteDataUrl = "https://example.com/route-data",
                    IsPublic = true,
                    IsDeleted = false
                });

                // Add users who favorited the journey
                context.JourneyFavorites.AddRange(
                    new JourneyFavorite { UserId = _userId1, JourneyId = _journeyId }
                    // Only one user this time, to test user filtering
                );

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new AppDbContext(options))
            {
                var service = new JourneyNotificationService(_mockHubContext.Object, context, _mockLogger.Object);
                await service.NotifyJourneyDeleted(_journeyId);
            }

            // Assert
            // Verify only one user group was called with the message
            _mockHubClients.Verify(
                clients => clients.Group(_userId1.ToString()),
                Times.Once);

            _mockHubClients.Verify(
                clients => clients.Group(_userId2.ToString()),
                Times.Never);

            // Verify the JourneyDeleted method was called on the client proxy
            _mockClientProxy.Verify(
                proxy => proxy.SendCoreAsync(
                    "JourneyDeleted",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyJourneyUpdated_WhenNoFavorites_ShouldNotSendNotifications()
        {
            // Arrange
            var journeyWithNoFavorites = Guid.NewGuid();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"NotificationTest_{Guid.NewGuid()}")
                .Options;

            // Seed the database with test data
            using (var context = new AppDbContext(options))
            {
                // Add a journey but no favorites
                context.Journeys.Add(new Journey
                {
                    Id = journeyWithNoFavorites,
                    Name = "Test Journey",
                    Description = "Test journey description",
                    StartLocation = "Start",
                    ArrivalLocation = "Arrival",
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    ArrivalTime = DateTime.UtcNow.AddHours(-1),
                    RouteDataUrl = "https://example.com/route-data",
                    IsPublic = true,
                    IsDeleted = false
                });

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new AppDbContext(options))
            {
                var service = new JourneyNotificationService(_mockHubContext.Object, context, _mockLogger.Object);
                await service.NotifyJourneyUpdated(journeyWithNoFavorites);
            }

            // Assert
            // Verify no groups were called
            _mockHubClients.Verify(
                clients => clients.Group(It.IsAny<string>()),
                Times.Never);

            // Verify no messages were sent
            _mockClientProxy.Verify(
                proxy => proxy.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SendFallbackNotificationAsync_ShouldCreateOutboxMessage()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"NotificationTest_{Guid.NewGuid()}")
                .Options;

            // Act
            using (var context = new AppDbContext(options))
            {
                var service = new JourneyNotificationService(_mockHubContext.Object, context, _mockLogger.Object);
                await service.SendFallbackNotificationAsync(_userId1, _journeyId, "JourneyUpdated");
            }

            // Assert
            // Verify that an outbox message was created
            using (var context = new AppDbContext(options))
            {
                var outboxMessage = await context.OutboxMessages
                    .FirstOrDefaultAsync(m => m.Type == "FallbackNotification");
                
                Assert.NotNull(outboxMessage);
                Assert.Contains(_userId1.ToString(), outboxMessage.Content);
                Assert.Contains(_journeyId.ToString(), outboxMessage.Content);
                Assert.Contains("JourneyUpdated", outboxMessage.Content);
            }
        }
    }
} 