using System.ComponentModel.DataAnnotations;

namespace NavigationPlatform.Infrastructure.Persistence.Outbox
{
    public class OutboxMessage
    {
        [Required]
        [MaxLength(500)]
        public string Type { get; set; } = default!;
    }
} 