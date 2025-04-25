using System;
using System.Threading.Tasks;

namespace NavigationPlatform.Application.Common.Interfaces
{
    public interface IJourneyNotificationService
    {
        Task NotifyJourneyUpdated(Guid journeyId);
        Task NotifyJourneyDeleted(Guid journeyId);
        Task SendFallbackNotificationAsync(Guid userId, Guid journeyId, string messageType);
    }
} 