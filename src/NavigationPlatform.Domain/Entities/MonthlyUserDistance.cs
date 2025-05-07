using System;

namespace NavigationPlatform.Domain.Entities
{
    public class MonthlyUserDistance : EntityBase
    {
        public Guid UserId { get; set; }
        
        public int Year { get; set; }
        
        public int Month { get; set; }
        
        public double TotalDistanceKm { get; set; }
        
        public int JourneyCount { get; set; }
        
        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
} 