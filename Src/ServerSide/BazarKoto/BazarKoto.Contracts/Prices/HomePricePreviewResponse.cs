namespace BazarKoto.Contracts.Prices;

public class HomePricePreviewResponse
{
    public string ProductNameEn { get; set; } = string.Empty;
    public string? ProductNameBn { get; set; }
    public string Market { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CategoryNameEn { get; set; }
    public string? CategoryNameBn { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Average { get; set; }
    public int Change { get; set; }
    public string Searchable { get; set; } = string.Empty;
}
