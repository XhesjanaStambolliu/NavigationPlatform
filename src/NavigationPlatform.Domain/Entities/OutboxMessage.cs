using System;
using System.ComponentModel.DataAnnotations;

namespace NavigationPlatform.Domain.Entities
{
    public class OutboxMessage : EntityBase
    {
        [Required, MaxLength(500)]
        public string Type { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime? ProcessedAt { get; set; }
        
        public string Error { get; set; } = string.Empty;
        
        public int RetryCount { get; set; }
        
        [MaxLength(50)]
        public string CorrelationId { get; set; } = string.Empty;
    }
} 