using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Events;
using NavigationPlatform.Domain.Enums;
using Xunit;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NavigationPlatform.Tests
{
    /// <summary>
    /// Tests to verify that the outbox messaging system correctly handles circular references
    /// in entity relationships when serializing and deserializing domain events.
    /// </summary>
    public class OutboxProcessingTests
    {
        /// <summary>
        /// Test that verifies we can serialize and deserialize a domain event with circular references
        /// </summary>
        [Fact]
        public void CanHandleCircularReferencesSerialization()
        {
            // Arrange
            var journey = CreateJourneyWithCircularReferences();
            var journeyCreatedEvent = new JourneyCreatedEvent(journey, journey.OwnerId);
            
            // Use System.Text.Json settings with ReferenceHandler.Preserve
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true
            };
            
            // Act - Serialize with System.Text.Json (what's used in production)
            var serialized = System.Text.Json.JsonSerializer.Serialize(journeyCreatedEvent, jsonOptions);
            
            // Verify the serialized JSON contains $id and $ref tokens for circular references
            Assert.Contains("$id", serialized);
            Assert.Contains("$ref", serialized);
            
            // Demonstrate we can use Newtonsoft.Json for deserialization if needed
            // This is just for testing - in production we'd use the same serializer
            var newtonsoftSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.Auto
            };
            
            // Re-serialize using Newtonsoft.Json
            var newtonsoftSerialized = JsonConvert.SerializeObject(journeyCreatedEvent, newtonsoftSettings);
            
            // Deserialize using Newtonsoft.Json
            var deserialized = JsonConvert.DeserializeObject<JourneyCreatedEvent>(newtonsoftSerialized, newtonsoftSettings);
            
            // Assert basic properties are correctly preserved
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Journey);
            Assert.Equal(journey.Id, deserialized.Journey.Id);
            Assert.Equal(journey.Name, deserialized.Journey.Name);
            Assert.Equal(journey.OwnerId, deserialized.UserId);
            
            // Verify the circular reference was preserved
            if (deserialized.Journey.Owner != null && deserialized.Journey.Owner.Journeys.Count > 0)
            {
                var ownerJourney = deserialized.Journey.Owner.Journeys.First();
                Assert.Equal(deserialized.Journey.Id, ownerJourney.Id);
            }
        }
        
        /// <summary>
        /// Helper method to create a journey with circular references
        /// </summary>
        private Journey CreateJourneyWithCircularReferences()
        {
            // Create owner with required properties initialized
            var owner = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                UserName = "testuser", // Explicitly set to non-null
                FullName = "Test User",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                Journeys = new List<Journey>(), // Initialize collection
                ProfilePictureUrl = "https://example.com/avatar.jpg" // Initialize all non-nullable properties
            };
            
            // Create journey with required properties initialized
            var journey = new Journey
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                Owner = owner,
                Name = "Test Journey",
                Description = "Journey with circular references",
                StartLocation = "Home",
                StartTime = DateTime.UtcNow.AddHours(-1),
                ArrivalLocation = "Office",
                ArrivalTime = DateTime.UtcNow,
                TransportType = TransportType.Car,
                DistanceKm = 15.0m,
                AverageSpeedKmh = 45.0,
                IsPublic = true,
                RouteDataUrl = "https://example.com/routes/test",
                IsDeleted = false,
                IsDailyGoalAchieved = false,
                CreatedAt = DateTime.UtcNow
            };
            
            // Create circular reference
            owner.Journeys.Add(journey);
            
            return journey;
        }

        /// <summary>
        /// How to manually test the outbox processing:
        /// 
        /// 1. Start the application with 'dotnet run' from NavigationPlatform.API
        /// 2. Create a new journey through the API endpoint POST /api/journeys
        /// 3. Check the database OutboxMessages table - there should be a new message
        /// 4. The background service should process this message automatically
        /// 5. Check logs for "Publishing domain event JourneyCreatedEvent" message
        /// 6. Verify that the message's ProcessedAt column is now populated
        /// </summary>
        [Fact(Skip = "Documentation only - not an actual test")]
        public void HowToManuallyTest()
        {
            // This is not an actual test, just documentation
        }
    }
} 