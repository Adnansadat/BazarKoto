namespace BazarKoto.Application.Features.Prices;

public class HomePricePreviewItem
{
    public string MarketName { get; set; } = string.Empty;
    public string ProductNameEn { get; set; } = string.Empty;
    public string ProductNameBn { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameBn { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal PricePerUnit { get; set; }
    public decimal? PreviousPricePerUnit { get; set; }
}
