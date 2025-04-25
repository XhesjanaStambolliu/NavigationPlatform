using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Domain.Entities
{
    public class JourneyShare : EntityBase
    {
        public Guid JourneyId { get; set; }
        
        public Guid UserId { get; set; }
        
        public ShareType ShareType { get; set; }
        
        [MaxLength(500)]
        public string ShareNote { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        // Navigation properties
        public virtual Journey Journey { get; set; }
        public virtual ApplicationUser SharedWithUser { get; set; }
        public virtual ICollection<ShareAudit> Audits { get; set; } = new List<ShareAudit>();
    }
} 