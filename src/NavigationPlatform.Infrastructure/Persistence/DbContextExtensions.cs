using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace NavigationPlatform.Infrastructure.Persistence
{
    public static class DbContextExtensions
    {
        public static IServiceCollection RegisterDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
                
                // Suppress the pending model changes warning
                options.ConfigureWarnings(warnings => 
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

            return services;
        }

        public static void ApplyMigrations(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<AppDbContext>>();
            var context = services.GetRequiredService<AppDbContext>();

            try
            {
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} database migrations", pendingMigrations.Count);
                    context.Database.Migrate();
                    logger.LogInformation("Database migrations applied successfully");
                }
                else
                {
                    // Check if database exists, if not create it
                    if (!context.Database.CanConnect())
                    {
                        logger.LogInformation("Creating database...");
                        context.Database.EnsureCreated();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying migrations");
                
                // Last resort - try to create database directly
                try
                {
                    logger.LogWarning("Attempting to create database directly as fallback...");
                    context.Database.EnsureCreated();
                }
                catch (Exception innerEx)
                {
                    logger.LogError(innerEx, "Failed to create database with fallback method");
                    throw;
                }
            }
        }
    }
} 