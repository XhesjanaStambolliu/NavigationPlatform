using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using NavigationPlatform.Domain.Enums;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Domain.Entities
{
    public class Journey : EntityBase
    {
        private readonly List<DomainEvent> _domainEvents = new List<DomainEvent>();
        
        [Required, MaxLength(100)]
        public string Name { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        public Guid OwnerId { get; set; }
        
        [Required, MaxLength(100)]
        public string StartLocation { get; set; }
        
        public DateTime StartTime { get; set; }
        
        [Required, MaxLength(100)]
        public string ArrivalLocation { get; set; }
        
        public DateTime ArrivalTime { get; set; }
        
        public TransportType TransportType { get; set; }
        
        public DistanceKm Distance { get; private set; }
        
        [NotMapped]
        [JsonIgnore]
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        
        // Backing field for EF Core to map to DistanceKm column
        private decimal _distanceKm;
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal DistanceKm 
        { 
            get => Distance?.Value ?? _distanceKm;
            set
            {
                _distanceKm = value;
                Distance = value != 0 ? new DistanceKm(value) : null;
            }
        }
        
        public bool IsPublic { get; set; }
        
        [MaxLength(1000)]
        public string RouteDataUrl { get; set; }
        
        public bool IsDeleted { get; set; }
        
        public bool IsDailyGoalAchieved { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual ApplicationUser Owner { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<JourneyShare> Shares { get; set; } = new List<JourneyShare>();
        
        [JsonIgnore]
        public virtual ICollection<PublicLink> PublicLinks { get; set; } = new List<PublicLink>();
        
        [JsonIgnore]
        public virtual ICollection<JourneyFavorite> FavoritedBy { get; set; } = new List<JourneyFavorite>();
        
        [JsonIgnore]
        public virtual ICollection<DailyDistanceBadge> DailyDistanceBadges { get; set; } = new List<DailyDistanceBadge>();
        
        public void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
        
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
        
        // Aggregate root methods
        public void UpdateDetails(string name, string description, string startLocation, 
            string arrivalLocation, DateTime startTime, DateTime arrivalTime, 
            TransportType transportType, decimal distanceKm, string routeDataUrl, 
            bool isPublic, Guid userId)
        {
            Name = name;
            Description = description;
            StartLocation = startLocation;
            ArrivalLocation = arrivalLocation;
            StartTime = startTime;
            ArrivalTime = arrivalTime;
            TransportType = transportType;
            DistanceKm = distanceKm;
            RouteDataUrl = routeDataUrl;
            IsPublic = isPublic;
            
            AddDomainEvent(new JourneyUpdatedEvent(this, userId));
        }
        
        public void MarkAsDeleted(Guid userId)
        {
            IsDeleted = true;
            AddDomainEvent(new JourneyDeletedEvent(Id, userId));
        }
        
        public void SetDailyGoalAchieved(Guid userId)
        {
            if (!IsDailyGoalAchieved)
            {
                IsDailyGoalAchieved = true;
                AddDomainEvent(new DailyGoalAchievedEvent(this, userId, this.DistanceKm, DateTime.UtcNow));
            }
        }
    }
} 