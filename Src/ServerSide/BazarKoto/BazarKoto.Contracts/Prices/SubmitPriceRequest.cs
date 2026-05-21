namespace BazarKoto.Contracts.Prices;

public class SubmitPriceRequest
{
    public Guid MarketId { get; set; }
    public Guid ProductId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal PricePerUnit { get; set; }
    public decimal? QuantityChecked { get; set; }
    public DateOnly PriceDate { get; set; }
    public TimeOnly? PriceTime { get; set; }
    public string SellerType { get; set; } = string.Empty;
    public string PriceSource { get; set; } = string.Empty;
    public string QualityGrade { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
