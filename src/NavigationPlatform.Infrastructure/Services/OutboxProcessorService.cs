using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NavigationPlatform.Application.Common.Interfaces;

namespace NavigationPlatform.Infrastructure.Services
{
    public class OutboxProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessorService> _logger;
        private readonly OutboxProcessorOptions _options;

        public OutboxProcessorService(
            IServiceProvider serviceProvider,
            IOptions<OutboxProcessorOptions> options,
            ILogger<OutboxProcessorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing outbox messages");
                }

                // Wait for the next polling interval
                await Task.Delay(_options.PollingInterval, stoppingToken);
            }
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            // Create a scope to resolve scoped services (DbContext, etc.)
            using var scope = _serviceProvider.CreateScope();
            var outboxProcessor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

            try
            {
                int processedCount = await outboxProcessor.ProcessMessageBatchAsync(_options.BatchSize, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during outbox message processing");
                throw;
            }
        }
    }
    
    public class OutboxProcessorOptions
    {
        /// <summary>
        /// The interval at which to poll for new outbox messages to process, in milliseconds.
        /// Default: 5 seconds
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// The maximum number of messages to process in a single batch.
        /// Default: 100
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
} 