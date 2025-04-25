using System.Threading;
using System.Threading.Tasks;
using NavigationPlatform.Domain.Events;

namespace NavigationPlatform.Application.Common.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : DomainEvent;
    }
} 