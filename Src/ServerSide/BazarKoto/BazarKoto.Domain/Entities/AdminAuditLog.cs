using System;
using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities
{
    public class AdminAuditLog : BaseEntity
    {
        public Guid? AdminUserId { get; set; }
        public User? AdminUser { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }
        public string IpHash { get; set; } = string.Empty;
        // CreatedAt comes from BaseEntity
    }
}
