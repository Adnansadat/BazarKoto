namespace BazarKoto.Contracts.Prices;

public class PublicProductPriceResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductNameEn { get; set; } = string.Empty;
    public string ProductNameBn { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameBn { get; set; } = string.Empty;
    public Guid MarketId { get; set; }
    public string MarketName { get; set; } = string.Empty;
    public Guid? DivisionId { get; set; }
    public string DivisionNameEn { get; set; } = string.Empty;
    public string DivisionNameBn { get; set; } = string.Empty;
    public Guid? DistrictId { get; set; }
    public string DistrictNameEn { get; set; } = string.Empty;
    public string DistrictNameBn { get; set; } = string.Empty;
    public Guid? UpazilaId { get; set; }
    public string UpazilaNameEn { get; set; } = string.Empty;
    public string UpazilaNameBn { get; set; } = string.Empty;
    public Guid? UnionOrWardId { get; set; }
    public string? UnionOrWardNameEn { get; set; }
    public string? UnionOrWardNameBn { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal PricePerUnit { get; set; }
    public decimal? QuantityChecked { get; set; }
    public DateOnly PriceDate { get; set; }
    public TimeOnly? PriceTime { get; set; }
    public string SellerType { get; set; } = string.Empty;
    public string PriceSource { get; set; } = string.Empty;
    public string QualityGrade { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
}
