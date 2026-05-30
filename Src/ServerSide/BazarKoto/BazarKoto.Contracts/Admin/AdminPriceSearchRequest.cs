namespace BazarKoto.Contracts.Admin;

public class AdminPriceSearchRequest
{
    private const int MaxPageSize = 10;
    private int _pageNumber = 1;
    private int _pageSize = 10;

    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? MarketId { get; set; }

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : Math.Min(value, MaxPageSize);
    }
}
