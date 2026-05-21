namespace BazarKoto.Contracts.Prices;

public class PriceSubmissionResponse
{
    public Guid Id { get; set; }
    public Guid MarketId { get; set; }
    public string MarketName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductNameEn { get; set; } = string.Empty;
    public string ProductNameBn { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameBn { get; set; } = string.Empty;
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
