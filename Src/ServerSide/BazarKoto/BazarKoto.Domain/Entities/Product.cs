using System;
using BazarKoto.Domain.Common;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Domain.Entities
{
    public class Product : AuditableEntity
    {
        public Guid CategoryId { get; set; }
        public ProductCategory? Category { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameBn { get; set; } = string.Empty;
        public string? LocalName { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string PrimaryUnit { get; set; } = string.Empty;
        public ProductState ProductState { get; set; }
        public string? Notes { get; set; }
        public RecordStatus Status { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
