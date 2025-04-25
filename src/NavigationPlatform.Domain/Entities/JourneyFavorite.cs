using System;

namespace NavigationPlatform.Domain.Entities
{
    public class JourneyFavorite : EntityBase
    {
        public Guid UserId { get; set; }
        
        public Guid JourneyId { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; }
        public virtual Journey Journey { get; set; }
    }
} 