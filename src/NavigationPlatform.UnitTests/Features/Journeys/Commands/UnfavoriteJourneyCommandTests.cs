using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Application.Features.Journeys.Commands.UnfavoriteJourney;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Infrastructure.Persistence;
using Xunit;

namespace NavigationPlatform.UnitTests.Features.Journeys.Commands
{
    public class UnfavoriteJourneyCommandTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Guid _authenticatedUserId = Guid.NewGuid();
        private readonly Guid _journeyId = Guid.NewGuid();
        private readonly Guid _ownerId = Guid.NewGuid();

        public UnfavoriteJourneyCommandTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_authenticatedUserId);
            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        }

        [Fact]
        public async Task Handle_ExistingFavorite_ShouldRemoveFromFavorites()
        {
            // Arrange
            var command = new UnfavoriteJourneyCommand { JourneyId = _journeyId };

            // Create in-memory database options
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"UnfavoriteJourneyTest_{Guid.NewGuid()}")
                .Options;

            // Create the database context and seed test data
            using (var context = new AppDbContext(options))
            {
                // Add a journey
                context.Journeys.Add(new Journey
                {
                    Id = _journeyId,
                    OwnerId = _ownerId,
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

                // Add existing favorite
                context.JourneyFavorites.Add(new JourneyFavorite
                {
                    JourneyId = _journeyId,
                    UserId = _authenticatedUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                });
                
                await context.SaveChangesAsync();
            }

            // Act
            ApiResponse response;
            using (var context = new AppDbContext(options))
            {
                var handler = new UnfavoriteJourneyCommandHandler(context, _mockCurrentUserService.Object);
                response = await handler.Handle(command, CancellationToken.None);
            }

            // Assert
            Assert.True(response.Success);

            // Verify favorite was removed
            using (var context = new AppDbContext(options))
            {
                var favorite = await context.JourneyFavorites
                    .FirstOrDefaultAsync(f => f.JourneyId == _journeyId && f.UserId == _authenticatedUserId);
                
                Assert.Null(favorite);
            }
        }

        [Fact]
        public async Task Handle_NonExistingFavorite_ShouldBeIdempotent()
        {
            // Arrange
            var command = new UnfavoriteJourneyCommand { JourneyId = _journeyId };

            // Create in-memory database options
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"UnfavoriteJourneyTest_{Guid.NewGuid()}")
                .Options;

            // Create the database context and seed test data
            using (var context = new AppDbContext(options))
            {
                // Add a journey but no favorite
                context.Journeys.Add(new Journey
                {
                    Id = _journeyId,
                    OwnerId = _ownerId,
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
            ApiResponse response;
            using (var context = new AppDbContext(options))
            {
                var handler = new UnfavoriteJourneyCommandHandler(context, _mockCurrentUserService.Object);
                response = await handler.Handle(command, CancellationToken.None);
            }

            // Assert
            Assert.True(response.Success);
        }

        [Fact]
        public async Task Handle_UnauthenticatedUser_ShouldReturnFailure()
        {
            // Arrange
            var command = new UnfavoriteJourneyCommand { JourneyId = _journeyId };
            var unauthenticatedUserService = new Mock<ICurrentUserService>();
            unauthenticatedUserService.Setup(s => s.IsAuthenticated).Returns(false);

            // Create in-memory database options
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"UnfavoriteJourneyTest_{Guid.NewGuid()}")
                .Options;

            // Act
            ApiResponse response;
            using (var context = new AppDbContext(options))
            {
                var handler = new UnfavoriteJourneyCommandHandler(context, unauthenticatedUserService.Object);
                response = await handler.Handle(command, CancellationToken.None);
            }

            // Assert
            Assert.False(response.Success);
            Assert.Contains("authenticated", response.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
} 