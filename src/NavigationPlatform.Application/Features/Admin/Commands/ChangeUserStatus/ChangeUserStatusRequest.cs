using System.ComponentModel.DataAnnotations;
using NavigationPlatform.Domain.Enums;

namespace NavigationPlatform.Application.Features.Admin.Commands.ChangeUserStatus
{
    public class ChangeUserStatusRequest
    {
        /// <summary>
        /// The status to set for the user
        /// </summary>
        [Required]
        public UserStatus Status { get; set; }
        
        /// <summary>
        /// The reason for changing the user's status
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }
    }
} 