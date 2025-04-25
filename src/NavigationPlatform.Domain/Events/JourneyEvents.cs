using System;
using System.Text.Json.Serialization;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Domain.Events
{
    public class JourneyCreatedEvent : DomainEvent
    {
        [JsonIgnore]
        public Journey Journey { get; }
        public Guid JourneyId { get; }
        public Guid UserId { get; }

        // Constructor for serialization
        [JsonConstructor]
        public JourneyCreatedEvent(Guid journeyId, Guid userId)
        {
            JourneyId = journeyId;
            UserId = userId;
        }

        // Main constructor
        public JourneyCreatedEvent(Journey journey, Guid userId)
        {
            Journey = journey;
            JourneyId = journey?.Id ?? Guid.Empty;
            UserId = userId;
        }
    }

    public class JourneyUpdatedEvent : DomainEvent
    {
        [JsonIgnore]
        public Journey Journey { get; }
        public Guid JourneyId { get; }
        public Guid UserId { get; }

        // Constructor for serialization
        [JsonConstructor]
        public JourneyUpdatedEvent(Guid journeyId, Guid userId)
        {
            JourneyId = journeyId;
            UserId = userId;
        }

        // Main constructor
        public JourneyUpdatedEvent(Journey journey, Guid userId)
        {
            Journey = journey;
            JourneyId = journey?.Id ?? Guid.Empty;
            UserId = userId;
        }
    }

    public class JourneyDeletedEvent : DomainEvent
    {
        public Guid JourneyId { get; }
        public Guid UserId { get; }

        [JsonConstructor]
        public JourneyDeletedEvent(Guid journeyId, Guid userId)
        {
            JourneyId = journeyId;
            UserId = userId;
        }
    }
} 