using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Journey> Journeys { get; }
        DbSet<ApplicationUser> Users { get; }
        DbSet<JourneyShare> JourneyShares { get; }
        DbSet<PublicLink> PublicLinks { get; }
        DbSet<JourneyFavorite> JourneyFavorites { get; }
        DbSet<OutboxMessage> OutboxMessages { get; }
        DbSet<DailyDistanceBadge> DailyDistanceBadges { get; }
        DbSet<ShareAudit> ShareAudits { get; }
        DbSet<UserStatusAudit> UserStatusAudits { get; }
        DbSet<MonthlyUserDistance> MonthlyUserDistances { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
} 