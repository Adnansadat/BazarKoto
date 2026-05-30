namespace BazarKoto.Contracts.Admin;

public class AdminUpdatePriceRequest
{
    public Guid? ProductId { get; set; }
    public Guid? MarketId { get; set; }
    public decimal? Price { get; set; }
    public string? Unit { get; set; }
    public DateOnly? PriceDate { get; set; }
    public TimeOnly? PriceTime { get; set; }
    public string? Status { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
}
