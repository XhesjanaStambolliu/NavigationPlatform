using Microsoft.EntityFrameworkCore.Diagnostics;
using Prometheus;
using System.Data.Common;

namespace NavigationPlatform.API.Services;

public class DatabaseMetricsInterceptor : DbCommandInterceptor
{
    private static readonly Histogram DatabaseCommandDuration = Metrics
        .CreateHistogram("ef_database_command_duration_seconds",
            "Duration of database commands in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
            });

    private static readonly Counter DatabaseCommandCounter = Metrics
        .CreateCounter("ef_database_command_total",
            "Total number of database commands",
            new CounterConfiguration
            {
                LabelNames = new[] { "command_type" }
            });

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        DatabaseCommandCounter.WithLabels("read").Inc();
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        DatabaseCommandCounter.WithLabels("read").Inc();
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        DatabaseCommandDuration.Observe(eventData.Duration.TotalSeconds);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        DatabaseCommandDuration.Observe(eventData.Duration.TotalSeconds);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        DatabaseCommandCounter.WithLabels("write").Inc();
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        DatabaseCommandCounter.WithLabels("write").Inc();
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        DatabaseCommandDuration.Observe(eventData.Duration.TotalSeconds);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        DatabaseCommandDuration.Observe(eventData.Duration.TotalSeconds);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }
} 