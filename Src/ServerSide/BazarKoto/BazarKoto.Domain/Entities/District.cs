using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities
{
    public class District : AuditableEntity
    {
        public Guid DivisionId { get; set; }
        public Division? Division { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameBn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? BbsCode { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<Upazila> Upazilas { get; set; } = [];
    }
}
