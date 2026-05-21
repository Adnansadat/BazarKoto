using System;
using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities
{
    public class Contributor : AuditableEntity
    {
        public string DisplayName { get; set; } = string.Empty;
        public string VisitorId { get; set; } = string.Empty;
        public int SubmissionCount { get; set; }
        public decimal TrustScore { get; set; }
        public bool IsBlocked { get; set; }
    }
}
