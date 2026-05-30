namespace BazarKoto.Contracts.Admin;

public class AdminPriceListResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<AdminPriceRecordResponse> Data { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public List<string> Errors { get; set; } = [];
}
