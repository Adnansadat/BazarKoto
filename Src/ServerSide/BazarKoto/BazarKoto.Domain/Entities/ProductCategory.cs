using System;
using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities
{
    public class ProductCategory : AuditableEntity
    {
        public string NameEn { get; set; } = string.Empty;
        public string NameBn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionBn { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
