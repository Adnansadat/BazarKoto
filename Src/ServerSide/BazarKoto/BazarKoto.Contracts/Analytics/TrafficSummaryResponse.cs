namespace BazarKoto.Contracts.Analytics;

public class TrafficSummaryResponse
{
    public int TotalVisits { get; set; }
    public int UniqueVisitors { get; set; }
    public int TodayVisits { get; set; }
    public int ThisWeekVisits { get; set; }
    public int ThisMonthVisits { get; set; }
}
