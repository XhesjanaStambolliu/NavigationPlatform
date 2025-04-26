using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using HealthChecks.UI.Client;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using Serilog.Enrichers.CorrelationId;
using OpenTelemetry.Exporter;

namespace NavigationPlatform.API.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        // Configure Serilog
        builder.AddSerilogLogging();
        
        // Configure Health Checks
        builder.AddHealthChecks();
        
        // Configure Prometheus Metrics
        builder.AddPrometheusMetrics();
        
        // Configure OpenTelemetry Tracing
        builder.AddOpenTelemetryTracing();
        
        return builder;
    }
    
    public static WebApplication UseObservability(this WebApplication app)
    {
        // Configure Health Checks endpoints
        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        
        app.MapHealthChecks("/readyz", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        
        // Configure Prometheus metrics endpoint
        app.UseMetricServer();
        app.UseHttpMetrics();
        
        return app;
    }
    
    private static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithCorrelationId()
                .Enrich.WithCorrelationIdHeader("X-Correlation-ID");
        });
        
        return builder;
    }
    
    private static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        var healthChecks = builder.Services.AddHealthChecks();
        
        // Add DB health check
        healthChecks.AddDbContextCheck<NavigationPlatform.Infrastructure.Persistence.AppDbContext>(
            name: "database",
            tags: new[] { "ready" });
        
        // Add self check
        healthChecks.AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), 
            tags: new[] { "ready" });
        
        return builder;
    }
    
    private static WebApplicationBuilder AddPrometheusMetrics(this WebApplicationBuilder builder)
    {
        // Add Prometheus for ASP.NET Core
        builder.Services.AddHttpClient();
        
        return builder;
    }
    
    private static WebApplicationBuilder AddOpenTelemetryTracing(this WebApplicationBuilder builder)
    {
        var openTelemetryConfig = builder.Configuration.GetSection("OpenTelemetry");
        var serviceName = openTelemetryConfig["ServiceName"] ?? "NavigationPlatform";
        var jaegerEnabled = openTelemetryConfig.GetSection("Jaeger")?.GetValue<bool>("Enabled") ?? false;
        
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Ensure we capture the correlation ID in the trace
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            if (request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
                            {
                                activity.SetTag("correlation_id", correlationId.ToString());
                            }
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetDbStatementForText = true;
                    })
                    .AddSource("NavigationPlatform.API");
                
                // Add Jaeger exporter if enabled
                if (jaegerEnabled)
                {
                    var jaegerEndpoint = openTelemetryConfig.GetSection("Jaeger")["Endpoint"] ?? "http://localhost:4317";
                    
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(jaegerEndpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            });
        
        return builder;
    }
} 