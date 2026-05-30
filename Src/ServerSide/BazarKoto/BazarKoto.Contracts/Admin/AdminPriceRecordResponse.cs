namespace BazarKoto.Contracts.Admin;

public class AdminPriceRecordResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductLocalName { get; set; }
    public string? AlternateName { get; set; }
    public Guid MarketId { get; set; }
    public string MarketName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}
