using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NavigationPlatform.Domain.Entities;
using NavigationPlatform.Domain.Enums;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        // Fixed GUIDs for reference entities
        private static readonly Guid MainUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        private static readonly Guid SecondUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        private static readonly Guid ThirdUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        private static readonly Guid AdminUserId = Guid.Parse("66666666-6666-6666-6666-666666666666"); // Admin user for status changes
        
        private static readonly Guid Journey1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Journey2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Journey3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid Journey4Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid Journey5Id = Guid.Parse("55555555-5555-5555-5555-555555555555");

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
            
            // Seed all entities
            try
            {
                // Always seed in the correct order (respect foreign key constraints)
                await SeedUsersAsync(dbContext, logger);
                await SeedJourneysAsync(dbContext, logger);
                await SeedJourneySharesAsync(dbContext, logger);
                await SeedShareAuditsAsync(dbContext, logger);
                await SeedMonthlyUserDistancesAsync(dbContext, logger);
                await SeedJourneyFavoritesAsync(dbContext, logger);
                await SeedPublicLinksAsync(dbContext, logger);
                await SeedOutboxMessagesAsync(dbContext, logger);
                await SeedDailyDistanceBadgesAsync(dbContext, logger);
                await SeedUserStatusAuditsAsync(dbContext, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }
        
        private static async Task SeedUsersAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if main user already exists
            if (await dbContext.Users.AnyAsync(u => u.Id == MainUserId))
            {
                logger.LogInformation("Main user already exists");
                return;
            }
            
            logger.LogInformation("Creating users");
            
            var users = new List<ApplicationUser>
            {
                // Main user
                new ApplicationUser
                {
                    Id = MainUserId,
                    Email = "xhesjana.stambolliu@gmail.com",
                    UserName = "xhesjana.stambolliu",
                    FullName = "Xhesjana Stambolliu",
                    ProfilePictureUrl = "https://ui-avatars.com/api/?name=Xhesjana+Stambolliu&background=random",
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                
                // Additional users for testing
                new ApplicationUser
                {
                    Id = SecondUserId,
                    Email = "john.doe@example.com",
                    UserName = "john.doe",
                    FullName = "John Doe",
                    ProfilePictureUrl = "https://ui-avatars.com/api/?name=John+Doe&background=random",
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                
                new ApplicationUser
                {
                    Id = ThirdUserId,
                    Email = "guest@gmail.com",
                    UserName = "guest",
                    FullName = "Guest",
                    ProfilePictureUrl = "https://ui-avatars.com/api/?name=Jane+Smith&background=random",
                    Status = UserStatus.Suspended, // Setting one user to suspended for testing
                    CreatedAt = DateTime.UtcNow
                },
                
                // Admin user for handling user status changes
                new ApplicationUser
                {
                    Id = AdminUserId,
                    Email = "admin@gmail.com",
                    UserName = "admin",
                    FullName = "Admin",
                    ProfilePictureUrl = "https://ui-avatars.com/api/?name=System+Administrator&background=random",
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            dbContext.Users.AddRange(users);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Users created successfully");
        }

        private static async Task SeedJourneysAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if journeys already exist
            if (await dbContext.Journeys.AnyAsync(j => j.Id == Journey1Id))
            {
                logger.LogInformation("Sample journeys already exist");
                return;
            }
            
            logger.LogInformation("Creating sample journeys");
            
            // Reference dates for consistent testing
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var lastWeek = today.AddDays(-7);
            
            var sampleJourneys = new List<Journey>
            {
                // Journey 1: Short distance today
                new Journey
                {
                    Id = Journey1Id,
                    OwnerId = MainUserId,
                    Name = "Morning Commute",
                    Description = "Regular commute to work",
                    StartLocation = "Home",
                    StartTime = today.AddHours(8),
                    ArrivalLocation = "Office",
                    ArrivalTime = today.AddHours(9),
                    TransportType = TransportType.Car,
                    DistanceKm = 12.5m,
                    AverageSpeedKmh = 35.0,
                    IsPublic = true,
                    RouteDataUrl = "https://example.com/routes/1",
                    IsDeleted = false,
                    IsDailyGoalAchieved = false,
                    CreatedAt = today.AddHours(9)
                },
                
                // Journey 2: Another journey today that makes total > 20km
                new Journey
                {
                    Id = Journey2Id,
                    OwnerId = MainUserId,
                    Name = "Evening Return",
                    Description = "Return trip from work",
                    StartLocation = "Office",
                    StartTime = today.AddHours(17),
                    ArrivalLocation = "Home",
                    ArrivalTime = today.AddHours(18),
                    TransportType = TransportType.Car,
                    DistanceKm = 12.5m, // Total for the day: 25km
                    AverageSpeedKmh = 32.0,
                    IsPublic = true,
                    RouteDataUrl = "https://example.com/routes/2",
                    IsDeleted = false,
                    IsDailyGoalAchieved = true, // This one triggered the achievement
                    CreatedAt = today.AddHours(18)
                },
                
                // Journey 3: Long trip for second user
                new Journey
                {
                    Id = Journey3Id,
                    OwnerId = SecondUserId,
                    Name = "Weekend Trip",
                    Description = "Trip to the mountains",
                    StartLocation = "Home",
                    StartTime = yesterday,
                    ArrivalLocation = "Mountain Resort",
                    ArrivalTime = yesterday.AddHours(3),
                    TransportType = TransportType.Car,
                    DistanceKm = 78.2m, 
                    AverageSpeedKmh = 65.0,
                    IsPublic = false,
                    RouteDataUrl = "https://example.com/routes/3",
                    IsDeleted = false,
                    IsDailyGoalAchieved = true,
                    CreatedAt = yesterday.AddHours(3)
                },
                
                // Journey 4: Air travel for main user
                new Journey
                {
                    Id = Journey4Id,
                    OwnerId = MainUserId,
                    Name = "Business Trip",
                    Description = "Flight to conference",
                    StartLocation = "Local Airport",
                    StartTime = lastWeek,
                    ArrivalLocation = "International Airport",
                    ArrivalTime = lastWeek.AddHours(5),
                    TransportType = TransportType.Airplane,
                    DistanceKm = 500.0m,
                    AverageSpeedKmh = 800.0,
                    IsPublic = true,
                    RouteDataUrl = "https://example.com/routes/4",
                    IsDeleted = false,
                    IsDailyGoalAchieved = true,
                    CreatedAt = lastWeek.AddHours(5)
                },
                
                // Journey 5: Short walk for third user
                new Journey
                {
                    Id = Journey5Id,
                    OwnerId = ThirdUserId,
                    Name = "Morning Walk",
                    Description = "Walk in the park",
                    StartLocation = "Home",
                    StartTime = yesterday.AddHours(7),
                    ArrivalLocation = "Park",
                    ArrivalTime = yesterday.AddHours(8),
                    TransportType = TransportType.Walk,
                    DistanceKm = 5.0m,
                    AverageSpeedKmh = 5.0,
                    IsPublic = true,
                    RouteDataUrl = "https://example.com/routes/5",
                    IsDeleted = false,
                    IsDailyGoalAchieved = false,
                    CreatedAt = yesterday.AddHours(8)
                }
            };
            
            dbContext.Journeys.AddRange(sampleJourneys);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample journeys created successfully");
        }

        private static async Task SeedJourneySharesAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if shares already exist
            if (await dbContext.JourneyShares.AnyAsync())
            {
                logger.LogInformation("Sample journey shares already exist");
                return;
            }
            
            logger.LogInformation("Creating sample journey shares");
            
            var shares = new List<JourneyShare>
            {
                // Share a journey from the main user to the second user
                new JourneyShare
                {
                    Id = Guid.NewGuid(),
                    JourneyId = Journey1Id,
                    UserId = SecondUserId,
                    ShareType = ShareType.ViewOnly,
                    ShareNote = "Sharing my commute route",
                    ExpiresAt = DateTime.UtcNow.AddMonths(1),
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                
                // Share a journey from the second user to the main user
                new JourneyShare
                {
                    Id = Guid.NewGuid(),
                    JourneyId = Journey3Id,
                    UserId = MainUserId,
                    ShareType = ShareType.Edit,
                    ShareNote = "Check out this mountain trip route",
                    ExpiresAt = null, // No expiration
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };
            
            dbContext.JourneyShares.AddRange(shares);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample journey shares created successfully");
        }
        
        private static async Task SeedShareAuditsAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if audit records already exist
            if (await dbContext.ShareAudits.AnyAsync())
            {
                logger.LogInformation("Sample share audits already exist");
                return;
            }
            
            // Get the first journey share to audit
            var share = await dbContext.JourneyShares.FirstOrDefaultAsync();
            if (share == null)
            {
                logger.LogWarning("No journey shares found for audit seeding");
                return;
            }
            
            logger.LogInformation("Creating sample share audits");
            
            var audits = new List<ShareAudit>
            {
                new ShareAudit
                {
                    Id = Guid.NewGuid(),
                    JourneyShareId = share.Id,
                    Action = "Created",
                    Details = "Share created with ViewOnly permissions",
                    IpAddress = "192.168.1.1",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    CreatedAt = share.CreatedAt
                },
                
                new ShareAudit
                {
                    Id = Guid.NewGuid(),
                    JourneyShareId = share.Id,
                    Action = "Viewed",
                    Details = "Share accessed by recipient",
                    IpAddress = "192.168.1.2",
                    UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
                    CreatedAt = share.CreatedAt.AddHours(2)
                }
            };
            
            dbContext.ShareAudits.AddRange(audits);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample share audits created successfully");
        }
        
        private static async Task SeedMonthlyUserDistancesAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if monthly distance records already exist
            if (await dbContext.MonthlyUserDistances.AnyAsync())
            {
                logger.LogInformation("Sample monthly distances already exist");
                return;
            }
            
            // Get current month and year
            var now = DateTime.UtcNow;
            var currentMonth = now.Month;
            var currentYear = now.Year;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;
            
            logger.LogInformation("Creating sample monthly user distances");
            
            var monthlyDistances = new List<MonthlyUserDistance>
            {
                // Current month for main user
                new MonthlyUserDistance
                {
                    Id = Guid.NewGuid(),
                    UserId = MainUserId,
                    Year = currentYear,
                    Month = currentMonth,
                    TotalDistanceKm = 525.0, // Includes all journeys this month
                    JourneyCount = 3,
                    AverageSpeedKmh = 289.0, // Average across different transport types
                    CreatedAt = now
                },
                
                // Last month for main user
                new MonthlyUserDistance
                {
                    Id = Guid.NewGuid(),
                    UserId = MainUserId,
                    Year = lastMonthYear,
                    Month = lastMonth,
                    TotalDistanceKm = 325.5,
                    JourneyCount = 5,
                    AverageSpeedKmh = 45.0,
                    CreatedAt = now
                },
                
                // Current month for second user
                new MonthlyUserDistance
                {
                    Id = Guid.NewGuid(),
                    UserId = SecondUserId,
                    Year = currentYear,
                    Month = currentMonth,
                    TotalDistanceKm = 78.2, // Just the mountain trip
                    JourneyCount = 1,
                    AverageSpeedKmh = 65.0,
                    CreatedAt = now
                },
                
                // Current month for third user - just below 20km threshold
                new MonthlyUserDistance
                {
                    Id = Guid.NewGuid(),
                    UserId = ThirdUserId,
                    Year = currentYear,
                    Month = currentMonth,
                    TotalDistanceKm = 19.8, // Just below the badge threshold
                    JourneyCount = 4,
                    AverageSpeedKmh = 5.0,
                    CreatedAt = now
                }
            };
            
            dbContext.MonthlyUserDistances.AddRange(monthlyDistances);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample monthly user distances created successfully");
        }

        private static async Task SeedJourneyFavoritesAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if favorites already exist
            if (await dbContext.JourneyFavorites.AnyAsync())
            {
                logger.LogInformation("Sample journey favorites already exist");
                return;
            }
            
            logger.LogInformation("Creating sample journey favorites");
            
            var favorites = new List<JourneyFavorite>
            {
                // Main user favorites a journey from the second user
                new JourneyFavorite
                {
                    UserId = MainUserId,
                    JourneyId = Journey3Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                
                // Second user favorites a journey from the main user
                new JourneyFavorite
                {
                    UserId = SecondUserId,
                    JourneyId = Journey4Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                
                // Third user favorites a journey from the main user
                new JourneyFavorite
                {
                    UserId = ThirdUserId,
                    JourneyId = Journey1Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };
            
            dbContext.JourneyFavorites.AddRange(favorites);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample journey favorites created successfully");
        }

        private static async Task SeedPublicLinksAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if public links already exist
            if (await dbContext.PublicLinks.AnyAsync())
            {
                logger.LogInformation("Sample public links already exist");
                return;
            }
            
            logger.LogInformation("Creating sample public links");
            
            var publicLinks = new List<PublicLink>
            {
                new PublicLink
                {
                    Id = Guid.NewGuid(),
                    JourneyId = Journey1Id,
                    Token = "morning-commute-public",
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    IsDisabled = false,
                    AccessCount = 5,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                
                new PublicLink
                {
                    Id = Guid.NewGuid(),
                    JourneyId = Journey4Id,
                    Token = "business-trip-public",
                    ExpiresAt = DateTime.UtcNow.AddDays(60),
                    IsDisabled = false,
                    AccessCount = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                }
            };
            
            dbContext.PublicLinks.AddRange(publicLinks);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample public links created successfully");
        }
        
        private static async Task SeedOutboxMessagesAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if outbox messages already exist
            if (await dbContext.OutboxMessages.AnyAsync())
            {
                logger.LogInformation("Sample outbox messages already exist");
                return;
            }
            
            logger.LogInformation("Creating sample outbox messages");

            // First make sure the journeys exist
            var journey1 = await dbContext.Journeys.FindAsync(Journey1Id);
            var journey2 = await dbContext.Journeys.FindAsync(Journey2Id);
            
            if (journey1 == null || journey2 == null)
            {
                logger.LogWarning("Required journeys for outbox messages not found");
                return;
            }

            // Configure JSON serializer options to handle circular references
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true
            };

            // Create a journey created event for serialization
            var journeyCreatedEvent = new JourneyCreatedEvent(journey1, MainUserId);
                
            // Create a daily goal achieved event for serialization
            var dailyGoalEvent = new DailyGoalAchievedEvent(
                journey2,
                MainUserId,
                25.0m,
                DateTime.UtcNow.Date);
            
            var outboxMessages = new List<OutboxMessage>
            {
                // Journey created event
                new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = journeyCreatedEvent.GetType().AssemblyQualifiedName ?? "Unknown",
                    Content = JsonSerializer.Serialize(journeyCreatedEvent, journeyCreatedEvent.GetType(), jsonOptions),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    ProcessedAt = DateTime.UtcNow.AddDays(-1).AddMinutes(1),
                    CorrelationId = journey1.Id.ToString()
                },
                
                // Daily goal achieved event
                new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = dailyGoalEvent.GetType().AssemblyQualifiedName ?? "Unknown",
                    Content = JsonSerializer.Serialize(dailyGoalEvent, dailyGoalEvent.GetType(), jsonOptions),
                    CreatedAt = DateTime.UtcNow.AddHours(-12),
                    ProcessedAt = DateTime.UtcNow.AddHours(-12).AddMinutes(1),
                    CorrelationId = journey2.Id.ToString()
                }
            };
            
            dbContext.OutboxMessages.AddRange(outboxMessages);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample outbox messages created successfully");
        }
        
        private static async Task SeedDailyDistanceBadgesAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if badges already exist
            if (await dbContext.DailyDistanceBadges.AnyAsync())
            {
                logger.LogInformation("Sample daily distance badges already exist");
                return;
            }
            
            // Use the same dates as journeys
            var today = DateTime.UtcNow.Date;
            var lastWeek = today.AddDays(-7);
            
            logger.LogInformation("Creating sample daily distance badges");
            
            var badges = new List<DailyDistanceBadge>
            {
                // Today's badge for main user (from the two journeys)
                new DailyDistanceBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = MainUserId,
                    JourneyId = Journey2Id, // The journey that triggered it
                    AwardDate = today,
                    TotalDistanceKm = 25.0m,
                    CreatedAt = today.AddHours(18).AddMinutes(5)
                },
                
                // Last week's badge for main user (from the flight)
                new DailyDistanceBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = MainUserId,
                    JourneyId = Journey4Id,
                    AwardDate = lastWeek,
                    TotalDistanceKm = 500.0m,
                    CreatedAt = lastWeek.AddHours(5).AddMinutes(5)
                },
                
                // Yesterday's badge for second user (from the mountain trip)
                new DailyDistanceBadge
                {
                    Id = Guid.NewGuid(),
                    UserId = SecondUserId,
                    JourneyId = Journey3Id,
                    AwardDate = today.AddDays(-1),
                    TotalDistanceKm = 78.2m,
                    CreatedAt = today.AddDays(-1).AddHours(3).AddMinutes(5)
                }
            };
            
            dbContext.DailyDistanceBadges.AddRange(badges);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample daily distance badges created successfully");
        }

        private static async Task SeedUserStatusAuditsAsync(AppDbContext dbContext, ILogger logger)
        {
            // Check if user status audits already exist
            if (await dbContext.UserStatusAudits.AnyAsync())
            {
                logger.LogInformation("Sample user status audits already exist");
                return;
            }
            
            // Verify that the admin user exists
            bool adminExists = await dbContext.Users.AnyAsync(u => u.Id == AdminUserId);
            if (!adminExists)
            {
                logger.LogWarning("Admin user not found. Cannot create user status audits with non-existent admin ID.");
                
                // Create the admin user if it doesn't exist
                var adminUser = new ApplicationUser
                {
                    Id = AdminUserId,
                    Email = "admin@example.com",
                    UserName = "admin",
                    FullName = "System Administrator",
                    ProfilePictureUrl = "https://ui-avatars.com/api/?name=System+Administrator&background=random",
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                
                dbContext.Users.Add(adminUser);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Admin user created for user status auditing");
            }
            
            // Verify that users being referenced exist
            bool thirdUserExists = await dbContext.Users.AnyAsync(u => u.Id == ThirdUserId);
            bool secondUserExists = await dbContext.Users.AnyAsync(u => u.Id == SecondUserId);
            
            if (!thirdUserExists || !secondUserExists)
            {
                logger.LogWarning("Required users for status audits don't exist. Skipping audit creation.");
                return;
            }
            
            logger.LogInformation("Creating sample user status audits");

            // Create audit records for user status changes
            var userStatusAudits = new List<UserStatusAudit>
            {
                // ThirdUserId has been suspended by the admin
                new UserStatusAudit
                {
                    Id = Guid.NewGuid(),
                    UserId = ThirdUserId,
                    OldStatus = UserStatus.Active,
                    NewStatus = UserStatus.Suspended,
                    Reason = "User violated platform terms of service - promotional content",
                    ChangedByUserId = AdminUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                
                // A previous temporary suspension for SecondUserId that was lifted
                new UserStatusAudit
                {
                    Id = Guid.NewGuid(),
                    UserId = SecondUserId,
                    OldStatus = UserStatus.Active,
                    NewStatus = UserStatus.Suspended,
                    Reason = "Temporary suspension pending investigation",
                    ChangedByUserId = AdminUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                
                // Suspension lifted for SecondUserId
                new UserStatusAudit
                {
                    Id = Guid.NewGuid(),
                    UserId = SecondUserId,
                    OldStatus = UserStatus.Suspended,
                    NewStatus = UserStatus.Active,
                    Reason = "Investigation completed, account restored",
                    ChangedByUserId = AdminUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-28)
                }
            };
            
            dbContext.UserStatusAudits.AddRange(userStatusAudits);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Sample user status audits created successfully");
        }
    }
} 