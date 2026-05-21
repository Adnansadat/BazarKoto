namespace BazarKoto.Contracts.Analytics;

public class AdReadinessResponse
{
    public bool IsReady { get; set; }
    public int TotalVisits { get; set; }
    public int UniqueVisitors { get; set; }
    public decimal ClickThroughRate { get; set; }
    public string Message { get; set; } = string.Empty;
}
