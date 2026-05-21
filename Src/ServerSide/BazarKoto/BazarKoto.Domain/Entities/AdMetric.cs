using System;
using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities
{
    public class AdMetric : AuditableEntity
    {
        public string PagePath { get; set; } = string.Empty;
        public int Impressions { get; set; }
        public int Clicks { get; set; }
        public decimal Ctr { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
