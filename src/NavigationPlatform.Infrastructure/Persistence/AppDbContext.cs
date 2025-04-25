using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Domain;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IApplicationDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Journey> Journeys { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<JourneyShare> JourneyShares { get; set; }
        public DbSet<PublicLink> PublicLinks { get; set; }
        public DbSet<JourneyFavorite> JourneyFavorites { get; set; }
        public DbSet<ShareAudit> ShareAudits { get; set; }
        public DbSet<UserStatusAudit> UserStatusAudits { get; set; }
        public DbSet<MonthlyUserDistance> MonthlyUserDistances { get; set; }
        public DbSet<DailyDistanceBadge> DailyDistanceBadges { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Add audit information before saving
            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entities
            ConfigureUserEntity(modelBuilder);
            ConfigureJourneyEntity(modelBuilder);
            ConfigureJourneyShareEntity(modelBuilder);
            ConfigurePublicLinkEntity(modelBuilder);
            ConfigureJourneyFavoriteEntity(modelBuilder);
            ConfigureShareAuditEntity(modelBuilder);
            ConfigureUserStatusAuditEntity(modelBuilder);
            ConfigureMonthlyUserDistanceEntity(modelBuilder);
            ConfigureOutboxMessageEntity(modelBuilder);
            ConfigureDailyDistanceBadgeEntity(modelBuilder);
        }

        private void ConfigureUserEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                
                entity.HasIndex(e => e.Email)
                    .IsUnique();
                
                entity.HasIndex(e => e.UserName)
                    .IsUnique();
                
                entity.HasMany(e => e.Journeys)
                    .WithOne(e => e.Owner)
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureJourneyEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Journey>(entity =>
            {
                entity.ToTable("Journeys");
                
                entity.HasIndex(e => e.OwnerId);
                
                entity.HasIndex(e => e.IsPublic);
                
                entity.HasIndex(e => new { e.OwnerId, e.IsDeleted });

                // Configure new fields
                entity.Property(e => e.StartLocation)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ArrivalLocation)
                    .IsRequired()
                    .HasMaxLength(100);

                // Configure value conversion for DistanceKm property
                entity.Property(e => e.DistanceKm)
                    .HasConversion(
                        v => v,
                        v => new NavigationPlatform.Domain.DistanceKm(v))
                    .HasColumnType("decimal(5,2)");
                    
                // Ignore the Distance value object property since we're using the backing field
                entity.Ignore(e => e.Distance);
            });
        }

        private void ConfigureJourneyShareEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JourneyShare>(entity =>
            {
                entity.ToTable("JourneyShares");
                
                entity.HasIndex(e => new { e.JourneyId, e.UserId })
                    .IsUnique();
                
                entity.HasOne(e => e.Journey)
                    .WithMany(e => e.Shares)
                    .HasForeignKey(e => e.JourneyId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.SharedWithUser)
                    .WithMany(e => e.SharedWithMe)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurePublicLinkEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PublicLink>(entity =>
            {
                entity.ToTable("PublicLinks");
                
                entity.HasIndex(e => e.Token)
                    .IsUnique();
                
                entity.HasIndex(e => e.JourneyId);
                
                entity.HasOne(e => e.Journey)
                    .WithMany(e => e.PublicLinks)
                    .HasForeignKey(e => e.JourneyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureJourneyFavoriteEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JourneyFavorite>(entity =>
            {
                entity.ToTable("JourneyFavorites");
                
                // Composite key
                entity.HasKey(e => new { e.UserId, e.JourneyId });
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Favorites)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Journey)
                    .WithMany(e => e.FavoritedBy)
                    .HasForeignKey(e => e.JourneyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureShareAuditEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShareAudit>(entity =>
            {
                entity.ToTable("ShareAudits");
                
                entity.HasIndex(e => e.JourneyShareId);
                
                entity.HasOne(e => e.JourneyShare)
                    .WithMany(js => js.Audits)
                    .HasForeignKey(e => e.JourneyShareId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureUserStatusAuditEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserStatusAudit>(entity =>
            {
                entity.ToTable("UserStatusAudits");
                
                entity.HasIndex(e => e.UserId);
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.StatusAudits)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ChangedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureMonthlyUserDistanceEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MonthlyUserDistance>(entity =>
            {
                entity.ToTable("MonthlyUserDistances");
                
                entity.HasIndex(e => new { e.UserId, e.Year, e.Month })
                    .IsUnique();
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.DistanceStatistics)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureOutboxMessageEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                
                entity.HasIndex(e => e.ProcessedAt);
                
                entity.HasIndex(e => e.Type);
                
                entity.Property(e => e.Content)
                    .IsRequired();
            });
        }

        private void ConfigureDailyDistanceBadgeEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DailyDistanceBadge>(entity =>
            {
                entity.ToTable("DailyDistanceBadges");
                
                entity.HasIndex(e => new { e.UserId, e.AwardDate })
                    .IsUnique();
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.DailyDistanceBadges)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Journey)
                    .WithMany(e => e.DailyDistanceBadges)
                    .HasForeignKey(e => e.JourneyId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.Property(e => e.TotalDistanceKm)
                    .HasColumnType("decimal(5,2)");
            });
        }
    }
} 