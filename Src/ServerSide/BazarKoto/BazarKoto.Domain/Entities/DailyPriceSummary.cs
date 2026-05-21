using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities;

public class DailyPriceSummary : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? MarketId { get; set; }
    public Market? Market { get; set; }

    public Guid DivisionId { get; set; }
    public Division? Division { get; set; }

    public Guid DistrictId { get; set; }
    public District? District { get; set; }

    public Guid UpazilaId { get; set; }
    public Upazila? Upazila { get; set; }

    public Guid? UnionOrWardId { get; set; }
    public UnionOrWard? UnionOrWard { get; set; }

    public DateOnly PriceDate { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public int SubmissionCount { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
