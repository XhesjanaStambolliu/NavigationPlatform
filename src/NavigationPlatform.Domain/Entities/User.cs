using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Domain.Entities
{
    public class ApplicationUser : EntityBase
    {
        [Required, MaxLength(100)]
        public string Email { get; set; }
        
        [Required, MaxLength(100)]
        public string UserName { get; set; }
        
        [MaxLength(200)]
        public string FullName { get; set; }
        
        public UserStatus Status { get; set; } = UserStatus.Active;
        
        [MaxLength(500)]
        public string ProfilePictureUrl { get; set; }
        
        public DateTime? LastLoginAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Journey> Journeys { get; set; } = new List<Journey>();
        public virtual ICollection<JourneyShare> SharedWithMe { get; set; } = new List<JourneyShare>();
        public virtual ICollection<JourneyFavorite> Favorites { get; set; } = new List<JourneyFavorite>();
        public virtual ICollection<UserStatusAudit> StatusAudits { get; set; } = new List<UserStatusAudit>();
        public virtual ICollection<MonthlyUserDistance> DistanceStatistics { get; set; } = new List<MonthlyUserDistance>();
        public virtual ICollection<DailyDistanceBadge> DailyDistanceBadges { get; set; } = new List<DailyDistanceBadge>();
    }
} 