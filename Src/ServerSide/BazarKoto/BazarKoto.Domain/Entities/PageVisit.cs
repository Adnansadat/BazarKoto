using System;
using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities
{
    public class PageVisit : AuditableEntity
    {
        public string Path { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public string VisitorId { get; set; } = string.Empty;
        public string IpHash { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string? Referrer { get; set; }
        public string DeviceType { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
    }
}
