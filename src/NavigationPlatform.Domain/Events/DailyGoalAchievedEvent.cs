using System;
using System.Text.Json.Serialization;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Domain.Events
{
    public class DailyGoalAchievedEvent : DomainEvent
    {
        [JsonIgnore]
        public Journey Journey { get; }
        public Guid JourneyId { get; }
        public Guid UserId { get; }
        public decimal TotalDailyDistanceKm { get; }
        public DateTime Date { get; }

        // Constructor for serialization
        [JsonConstructor]
        public DailyGoalAchievedEvent(Guid journeyId, Guid userId, decimal totalDailyDistanceKm, DateTime date)
        {
            JourneyId = journeyId;
            UserId = userId;
            TotalDailyDistanceKm = totalDailyDistanceKm;
            Date = date;
        }

        // Main constructor
        public DailyGoalAchievedEvent(Journey journey, Guid userId, decimal totalDailyDistanceKm, DateTime date)
        {
            Journey = journey;
            JourneyId = journey?.Id ?? Guid.Empty;
            UserId = userId;
            TotalDailyDistanceKm = totalDailyDistanceKm;
            Date = date;
        }
    }
} 