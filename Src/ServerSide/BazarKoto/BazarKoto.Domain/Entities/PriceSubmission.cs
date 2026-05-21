using System;
using BazarKoto.Domain.Common;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Domain.Entities
{
    public class PriceSubmission : AuditableEntity
    {
        public Guid MarketId { get; set; }
        public Market? Market { get; set; }

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public string Unit { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
        public decimal? QuantityChecked { get; set; }
        public DateOnly PriceDate { get; set; }
        public TimeOnly? PriceTime { get; set; }
        public SellerType SellerType { get; set; }
        public PriceSource PriceSource { get; set; }
        public QualityGrade QualityGrade { get; set; }
        public string? Notes { get; set; }
        public Guid? SubmittedByUserId { get; set; }
        public User? SubmittedByUser { get; set; }
        public SubmissionStatus Status { get; set; }
    }
}
