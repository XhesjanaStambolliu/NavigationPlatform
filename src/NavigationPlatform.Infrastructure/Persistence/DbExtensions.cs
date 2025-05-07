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
            }
        }

        private static async Task ApplyMigrationsAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Applying database migrations");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply database migrations");
                // Log but don't throw to prevent app crash
            }
        }
    }
} 