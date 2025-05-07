using System;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Application.Features.Journeys.Queries.Models
{
    public class JourneyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string StartLocation { get; set; }
        public DateTime StartTime { get; set; }
        public string ArrivalLocation { get; set; }
        public DateTime ArrivalTime { get; set; }
        public TransportType TransportType { get; set; }
        public string TransportTypeName => TransportType.ToString();
        public decimal DistanceKm { get; set; }
        public bool IsPublic { get; set; }
        public string RouteDataUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsFavorite { get; set; }
    }
} 