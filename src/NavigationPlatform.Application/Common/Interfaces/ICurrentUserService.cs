using System;

namespace NavigationPlatform.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string UserName { get; }
        bool IsAuthenticated { get; }
        string IpAddress { get; }
        string UserAgent { get; }
    }
} 