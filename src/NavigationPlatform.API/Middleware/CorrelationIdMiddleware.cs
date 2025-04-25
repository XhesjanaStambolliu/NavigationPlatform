using System.Diagnostics;

namespace NavigationPlatform.API.Middleware;

public class CorrelationIdOptions
{
    public string Header { get; set; } = "X-Correlation-ID";
    public bool IncludeInResponse { get; set; } = true;
}

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationIdOptions _options;
    
    public CorrelationIdMiddleware(RequestDelegate next, CorrelationIdOptions options)
    {
        _next = next;
        _options = options ?? new CorrelationIdOptions();
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetOrCreateCorrelationId(context);
        
        // Add the correlation ID to the Response headers if configured
        if (_options.IncludeInResponse)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append(_options.Header, correlationId);
                return Task.CompletedTask;
            });
        }
        
        // Add the correlation ID to the logging context
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Add to Activity
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetTag("correlation.id", correlationId);
            }
            
            // Continue with the request pipeline
            await _next(context);
        }
    }
    
    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.Header, out var correlationId) && 
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId!;
        }
        
        return Guid.NewGuid().ToString();
    }
}

// Extension methods
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>(new CorrelationIdOptions());
    }
    
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder, CorrelationIdOptions options)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>(options);
    }
} 