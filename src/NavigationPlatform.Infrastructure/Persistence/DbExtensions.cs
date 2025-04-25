using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NavigationPlatform.Infrastructure.Persistence
{
    public static class DbExtensions
    {
        public static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
                
                // Add database metrics interceptor if available
                // Note: The interceptor will be registered by the API project to avoid circular dependencies
                var interceptors = provider.GetServices<DbCommandInterceptor>();
                foreach (var interceptor in interceptors)
                {
                    options.AddInterceptors(interceptor);
                }
            });
            
            return services;
        }
        
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                logger.LogInformation("Initializing database...");
                var context = services.GetRequiredService<AppDbContext>();

                // Apply migrations
                await ApplyMigrationsAsync(context, logger);

                // Seed data
                await DbSeeder.SeedAsync(serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
                // Swallow the exception to avoid crashing the app
                // The app should continue to run and retry later
            }
        }

        private static async Task ApplyMigrationsAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
            }
            catch (SqlException ex) when (ex.Number == 2714)
            {
                // Handle object already exists error (common during parallel deployments)
                logger.LogWarning(ex, "Database schema conflict detected during migration");
                
                // Option to reset database if enabled
                if (Environment.GetEnvironmentVariable("RESET_DATABASE_ON_MIGRATION_ERROR")?.ToLower() == "true")
                {
                    logger.LogWarning("Resetting database due to migration conflicts...");
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.EnsureCreatedAsync();
                }
                else
                {
                    logger.LogWarning("Migration error occurred but database reset is disabled");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply database migrations");
                // Log but don't throw to prevent app crash
            }
        }
    }
} 