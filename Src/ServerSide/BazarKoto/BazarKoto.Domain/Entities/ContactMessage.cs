using System;
using BazarKoto.Domain.Common;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Domain.Entities
{
    public class ContactMessage : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ContactMessageStatus Status { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
