namespace BazarKoto.Contracts.Analytics;

public class TrackPageVisitRequest
{
    public string Path { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public string VisitorId { get; set; } = string.Empty;
    public string? Referrer { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string? Country { get; set; }
}
