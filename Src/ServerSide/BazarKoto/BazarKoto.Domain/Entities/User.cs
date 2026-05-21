using System;
using BazarKoto.Domain.Common;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Domain.Entities
{
    public class User : AuditableEntity
    {
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Admin;
        public bool IsActive { get; set; } = true;
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
