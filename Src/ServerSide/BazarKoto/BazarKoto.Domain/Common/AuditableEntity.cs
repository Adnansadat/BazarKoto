using System;

namespace BazarKoto.Domain.Common
{
    public class AuditableEntity : BaseEntity
    {
        public DateTime? UpdatedAt { get; set; }
    }
}
