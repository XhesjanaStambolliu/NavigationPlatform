using System;
using System.ComponentModel.DataAnnotations;

namespace NavigationPlatform.Domain.Entities
{
    public class PublicLink : EntityBase
    {
        public Guid JourneyId { get; set; }
        
        [Required, MaxLength(100)]
        public string Token { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsDisabled { get; set; }
        
        public int AccessCount { get; set; }
        
        // Navigation property
        public virtual Journey Journey { get; set; }
    }
} 