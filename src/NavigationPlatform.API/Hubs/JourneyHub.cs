using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NavigationPlatform.Application.Common.Interfaces;

namespace NavigationPlatform.API.Hubs
{
    [Authorize]
    public class JourneyHub : Hub
    {
        private readonly ICurrentUserService _currentUserService;

        public JourneyHub(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override async Task OnConnectedAsync()
        {
            // When a user connects, add them to their own user group
            if (_currentUserService.IsAuthenticated)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, _currentUserService.UserId.ToString());
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // When a user disconnects, remove them from their user group
            if (_currentUserService.IsAuthenticated)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, _currentUserService.UserId.ToString());
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
} 