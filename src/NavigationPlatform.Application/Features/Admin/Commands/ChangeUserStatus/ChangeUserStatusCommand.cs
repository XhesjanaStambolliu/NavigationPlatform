using System;
using MediatR;
using NavigationPlatform.Application.Common.Models;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Application.Features.Admin.Commands.ChangeUserStatus
{
    public class ChangeUserStatusCommand : IRequest<ApiResponse<bool>>
    {
        /// <summary>
        /// ID of the user to change status for
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// The new status to set
        /// </summary>
        public UserStatus Status { get; set; }
        
        /// <summary>
        /// ID of the admin making the change
        /// </summary>
        public Guid AdminId { get; set; }
        
        /// <summary>
        /// The reason for changing the user's status
        /// </summary>
        public string Reason { get; set; }
    }
} 