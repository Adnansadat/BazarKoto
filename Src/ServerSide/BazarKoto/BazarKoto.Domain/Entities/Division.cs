using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities
{
    public class Division : AuditableEntity
    {
        public string NameEn { get; set; } = string.Empty;
        public string NameBn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? BbsCode { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<District> Districts { get; set; } = [];
    }
}
