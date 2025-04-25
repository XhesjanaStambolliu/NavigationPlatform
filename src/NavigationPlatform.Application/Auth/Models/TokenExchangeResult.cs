using System;

namespace NavigationPlatform.Application.Auth.Models
{
    public class TokenExchangeResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public DateTime ExpiresAt => DateTime.UtcNow.AddSeconds(ExpiresIn);
        public string Scope { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string Error { get; set; } = string.Empty;
        public string ErrorDescription { get; set; } = string.Empty;
    }
} 