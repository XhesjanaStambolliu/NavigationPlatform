using System;
using MediatR;

namespace NavigationPlatform.Domain.Events
{
    public abstract class DomainEvent : INotification
    {
        public Guid Id { get; }
        public DateTime OccurredOn { get; }

        protected DomainEvent()
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
        }
    }
} 