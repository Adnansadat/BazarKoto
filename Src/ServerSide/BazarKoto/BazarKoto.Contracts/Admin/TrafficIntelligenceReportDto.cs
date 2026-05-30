namespace BazarKoto.Contracts.Admin;

public class TrafficIntelligenceReportDto
{
    public int TotalTraffic { get; set; }
    public int? MonthlyTraffic { get; set; }
    public int TodayVisitors { get; set; }
    public int? UniqueVisitorsToday { get; set; }
    public string PeakHourLabel { get; set; } = "Not available";
    public int? PeakHourVisits { get; set; }
    public int WeeklyTraffic { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string DataSourceLabel { get; set; } = "Live Backend Data";
    public string DataScopeNote { get; set; } =
        "Some metrics may be based on currently tracked analytics records available to the dashboard.";
}
