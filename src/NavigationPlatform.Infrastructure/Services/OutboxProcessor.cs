using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Infrastructure.Services
{
    public class OutboxProcessor : IOutboxProcessor
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public OutboxProcessor(
            IApplicationDbContext dbContext,
            IMediator mediator,
            ILogger<OutboxProcessor> logger)
        {
            _dbContext = dbContext;
            _mediator = mediator;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true
            };
        }

        public async Task<int> ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
        {
            int processedCount = 0;
            int batchSize = 100;
            bool hasMoreMessages = true;

            while (hasMoreMessages && !cancellationToken.IsCancellationRequested)
            {
                int batchProcessedCount = await ProcessMessageBatchAsync(batchSize, cancellationToken);
                processedCount += batchProcessedCount;
                
                // If we processed fewer messages than the batch size, we've processed all pending messages
                hasMoreMessages = batchProcessedCount == batchSize;
            }

            return processedCount;
        }

        public async Task<int> ProcessMessageBatchAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            // Find messages that have not been processed yet
            var messages = await _dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            int processedCount = 0;

            foreach (var message in messages)
            {
                try
                {
                    await ProcessMessageAsync(message, cancellationToken);
                    
                    // Mark as processed
                    message.ProcessedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    
                    processedCount++;
                }
                catch (Exception ex)
                {
                    // Log the error and update the message with error information
                    _logger.LogError(ex, "Error processing outbox message {MessageId} of type {MessageType}: {ErrorMessage}",
                        message.Id, message.Type, ex.Message);
                    
                    message.Error = $"{ex.GetType().Name}: {ex.Message}";
                    message.RetryCount++;
                    
                    // Only mark as processed if we've reached the retry limit (e.g., 3 attempts)
                    if (message.RetryCount >= 3)
                    {
                        message.ProcessedAt = DateTime.UtcNow;
                        _logger.LogWarning("Outbox message {MessageId} of type {MessageType} has failed {RetryCount} times and will not be retried",
                            message.Id, message.Type, message.RetryCount);
                    }
                    
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            return processedCount;
        }

        private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(message.Type))
            {
                throw new InvalidOperationException($"Message type is missing for message ID {message.Id}");
            }

            // Find the event type from the stored type name
            Type? eventType = Type.GetType(message.Type);
            if (eventType == null)
            {
                throw new InvalidOperationException($"Could not find type {message.Type} for message ID {message.Id}");
            }

            // Make sure it's a domain event
            if (!typeof(DomainEvent).IsAssignableFrom(eventType))
            {
                throw new InvalidOperationException($"Type {message.Type} does not inherit from DomainEvent");
            }

            // Deserialize the event
            object? domainEvent = JsonSerializer.Deserialize(message.Content, eventType, _jsonOptions);
            if (domainEvent == null)
            {
                throw new InvalidOperationException($"Failed to deserialize message content for message ID {message.Id}");
            }

            // Publish the event via MediatR
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
} 