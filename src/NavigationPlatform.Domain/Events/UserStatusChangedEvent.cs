using System;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Domain.Events
{
    public class UserStatusChangedEvent : DomainEvent
    {
        public Guid UserId { get; }
        public UserStatus OldStatus { get; }
        public UserStatus NewStatus { get; }
        public Guid ChangedByAdminId { get; }
        public DateTime Timestamp { get; }

        public UserStatusChangedEvent(
            Guid userId, 
            UserStatus oldStatus, 
            UserStatus newStatus, 
            Guid changedByAdminId)
        {
            UserId = userId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            ChangedByAdminId = changedByAdminId;
            Timestamp = DateTime.UtcNow;
        }
    }
} 