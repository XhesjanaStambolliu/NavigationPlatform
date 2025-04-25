using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Infrastructure.Persistence;
using NavigationPlatform.Infrastructure.Services;
using System;
using NavigationPlatform.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace NavigationPlatform.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext is now handled by AddAppDbContext in Program.cs
            
            // Register DbContext as IApplicationDbContext
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());
            
            // Register HttpContextAccessor (required for CurrentUserService)
            services.AddHttpContextAccessor();
            
            // Register services
            services.AddScoped<IEventPublisher, EventPublisher>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();

            // Register authorization handlers
            services.AddTransient<IAuthorizationHandler, JourneyAuthorizationHandler>();
            services.AddAuthorizationCore(options =>
            {
                options.AddPolicy("JourneyOwnerOrShared", policy =>
                    policy.Requirements.Add(new JourneyAuthorizationRequirement()));
            });

            return services;
        }
        
        public static IServiceCollection AddOutboxProcessor(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure OutboxProcessorOptions from configuration (if present)
            services.Configure<OutboxProcessorOptions>(options =>
            {
                var pollingIntervalMs = configuration.GetValue<int?>("OutboxProcessor:PollingIntervalMs");
                if (pollingIntervalMs.HasValue)
                {
                    options.PollingInterval = TimeSpan.FromMilliseconds(pollingIntervalMs.Value);
                }
                
                var batchSize = configuration.GetValue<int?>("OutboxProcessor:BatchSize");
                if (batchSize.HasValue)
                {
                    options.BatchSize = batchSize.Value;
                }
            });
            
            // Register OutboxProcessorService as a hosted service
            services.AddHostedService<OutboxProcessorService>();
            
            return services;
        }
    }
} 