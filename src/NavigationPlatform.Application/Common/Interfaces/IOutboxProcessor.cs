using System.Threading;
using System.Threading.Tasks;

namespace NavigationPlatform.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for processing outbox messages stored in the database.
    /// Implementations should handle retrieval, deserialization, and delivery of messages.
    /// </summary>
    public interface IOutboxProcessor
    {
        /// <summary>
        /// Processes all pending outbox messages that have not been processed yet.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop processing.</param>
        /// <returns>The number of messages successfully processed.</returns>
        Task<int> ProcessPendingMessagesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Processes a specific batch of outbox messages.
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to process in this batch.</param>
        /// <param name="cancellationToken">Cancellation token to stop processing.</param>
        /// <returns>The number of messages successfully processed.</returns>
        Task<int> ProcessMessageBatchAsync(int batchSize, CancellationToken cancellationToken = default);
    }
} 