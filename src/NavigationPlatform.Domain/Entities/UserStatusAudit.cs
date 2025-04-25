using System;
using System.ComponentModel.DataAnnotations;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Domain.Entities
{
    public class UserStatusAudit : EntityBase
    {
        public Guid UserId { get; set; }
        
        public UserStatus OldStatus { get; set; }
        
        public UserStatus NewStatus { get; set; }
        
        [MaxLength(500)]
        public string Reason { get; set; }
        
        public Guid? ChangedByUserId { get; set; }
        
        // Navigation property
        public virtual ApplicationUser User { get; set; }
        public virtual ApplicationUser ChangedByUser { get; set; }
    }
} 