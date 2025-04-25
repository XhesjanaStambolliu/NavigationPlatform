using System;
using System.ComponentModel.DataAnnotations;

namespace NavigationPlatform.Domain.Entities
{
    public class ShareAudit : EntityBase
    {
        public Guid JourneyShareId { get; set; }
        
        [Required, MaxLength(50)]
        public string Action { get; set; }
        
        [MaxLength(500)]
        public string Details { get; set; }
        
        public string IpAddress { get; set; }
        
        public string UserAgent { get; set; }
        
        // Navigation property
        public virtual JourneyShare JourneyShare { get; set; }
    }
} 