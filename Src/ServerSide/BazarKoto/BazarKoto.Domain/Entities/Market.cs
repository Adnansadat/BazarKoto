using System;
using BazarKoto.Domain.Common;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Domain.Entities
{
    public class Market : AuditableEntity
    {
        public Guid DivisionId { get; set; }
        public Division? Division { get; set; }
        public Guid DistrictId { get; set; }
        public District? District { get; set; }
        public Guid UpazilaId { get; set; }
        public Upazila? Upazila { get; set; }
        public Guid? UnionOrWardId { get; set; }
        public UnionOrWard? UnionOrWard { get; set; }
        public string Area { get; set; } = string.Empty;
        public string MarketName { get; set; } = string.Empty;
        public string VillageOrMoholla { get; set; } = string.Empty;
        public string Landmark { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public MarketType MarketType { get; set; }
        public OperatingSchedule OperatingSchedule { get; set; }
        public RecordStatus Status { get; set; }
    }
}
