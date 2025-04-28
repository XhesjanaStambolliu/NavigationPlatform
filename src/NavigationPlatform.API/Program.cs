using NavigationPlatform.API;
using NavigationPlatform.API.Extensions;
using NavigationPlatform.API.Hubs;
using NavigationPlatform.API.Middleware;
using NavigationPlatform.API.Services;
using NavigationPlatform.Application;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Infrastructure;
using NavigationPlatform.Infrastructure.Auth;
using NavigationPlatform.Infrastructure.OpenApi;
using NavigationPlatform.Infrastructure.Persistence;


var builder = WebApplication.CreateBuilder(args);

// Configure observability (Serilog, Health Checks, Prometheus, OpenTelemetry)
builder.AddObservability();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Add Application layer services
builder.Services.AddApplication();

// Add Infrastructure layer services
builder.Services.AddInfrastructure(builder.Configuration);

// Register OutboxProcessor background service
//builder.Services.AddOutboxProcessor(builder.Configuration);

// Load Auth0 settings from configuration
var auth0Section = builder.Configuration.GetSection("Auth0");

// Configure Auth0 settings
builder.Services.Configure<Auth0Settings>(auth0Section);

// Configure Swagger with Auth0 authentication
builder.Services.AddSwaggerWithAuth0(builder.Configuration);

// Configure Auth0 cookie-based authentication with token refresh
builder.Services.AddAuth0CookieAuthentication(builder.Configuration);

// Add authorization policies
builder.Services.AddAuthorizationPolicies();

// Add the DbContext to the container - use fully qualified method to resolve ambiguity
NavigationPlatform.Infrastructure.Persistence.DbExtensions.AddAppDbContext(builder.Services, builder.Configuration);

// Add SignalR
builder.Services.AddSignalR();

// Register notification service
builder.Services.AddScoped<IJourneyNotificationService, JourneyNotificationService>();

// Register database metrics interceptor
builder.Services.AddScoped<DatabaseMetricsInterceptor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithAuth0();
}

// Configure observability middleware
app.UseObservability();

// Add correlation ID middleware (after Serilog request logging, before other middleware)
app.UseCorrelationId(new NavigationPlatform.API.Middleware.CorrelationIdOptions
{
    Header = "X-Correlation-ID",
    IncludeInResponse = true
});

// Add global exception handling
app.UseExceptionHandling();

app.UseHttpsRedirection();

// Add token refresh middleware before authentication
app.UseTokenRefreshMiddleware();

app.UseAuthentication();
app.UseAuthorization();

// Add user status validation middleware after authentication
app.UseUserStatusValidation();

// Initialize database (apply migrations and seed data)
await app.Services.InitializeDatabaseAsync();

app.MapControllers();

// Map SignalR hub
app.MapHub<JourneyHub>("/hubs/journey");

app.Run();
