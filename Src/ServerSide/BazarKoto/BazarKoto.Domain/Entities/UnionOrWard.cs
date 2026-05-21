using BazarKoto.Domain.Common;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Domain.Entities
{
    public class UnionOrWard : AuditableEntity
    {
        public Guid UpazilaId { get; set; }
        public Upazila? Upazila { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameBn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? BbsCode { get; set; }
        public UnionOrWardType Type { get; set; } = UnionOrWardType.Unknown;
        public bool IsActive { get; set; } = true;
    }
}
