using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Infrastructure.Services
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;
        private readonly JsonSerializerOptions _jsonOptions;

        public EventPublisher(IApplicationDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true
            };
        }

        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : DomainEvent
        {
            // First, directly notify any in-memory handlers via MediatR
            await _mediator.Publish(domainEvent, cancellationToken);
            
            // Then persist the event to the outbox for reliable messaging
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = domainEvent.GetType().AssemblyQualifiedName,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions),
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };
            
            _dbContext.OutboxMessages.Add(outboxMessage);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
} 