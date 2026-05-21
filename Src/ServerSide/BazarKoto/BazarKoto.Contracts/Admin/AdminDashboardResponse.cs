using BazarKoto.Contracts.Analytics;

namespace BazarKoto.Contracts.Admin;

public class AdminDashboardResponse
{
    public TrafficSummaryResponse Traffic { get; set; } = new();
    public AdminMetricResponse Records { get; set; } = new();
    public ModerationQueueResponse Moderation { get; set; } = new();
    public List<PeakHourResponse> PeakHours { get; set; } = [];
}
