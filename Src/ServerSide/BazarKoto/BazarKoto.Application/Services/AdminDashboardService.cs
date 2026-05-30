using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Analytics;
using BazarKoto.Contracts.Admin;
using BazarKoto.Contracts.Common;

namespace BazarKoto.Application.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private static readonly TimeSpan BangladeshUtcOffset = TimeSpan.FromHours(6);
    private readonly IMarketRepository _marketRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IContactRepository _contactRepository;

    public AdminDashboardService(
        IMarketRepository marketRepository,
        IProductRepository productRepository,
        IPriceRepository priceRepository,
        IAnalyticsRepository analyticsRepository,
        IContactRepository contactRepository)
    {
        _marketRepository = marketRepository;
        _productRepository = productRepository;
        _priceRepository = priceRepository;
        _analyticsRepository = analyticsRepository;
        _contactRepository = contactRepository;
    }

    public async Task<ApiResponse<AdminDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = await BuildDashboardAsync(cancellationToken);

        return ApiResponse<AdminDashboardResponse>.Ok(dashboard);
    }

    public async Task<TrafficIntelligenceReportDto> GetTrafficIntelligenceReportAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = await BuildDashboardAsync(cancellationToken);
        var peakHour = dashboard.PeakHours.FirstOrDefault();

        return new TrafficIntelligenceReportDto
        {
            TotalTraffic = dashboard.Traffic.TotalVisits,
            MonthlyTraffic = dashboard.Traffic.ThisMonthVisits,
            TodayVisitors = dashboard.Traffic.TodayVisits,
            UniqueVisitorsToday = dashboard.Traffic.UniqueVisitorsToday,
            PeakHourLabel = peakHour is null ? "Not available" : FormatHourRange(peakHour.Hour),
            PeakHourVisits = peakHour?.VisitCount,
            WeeklyTraffic = dashboard.Traffic.ThisWeekVisits,
            GeneratedAt = DateTime.UtcNow,
            DataSourceLabel = "Live Backend Data"
        };
    }

    private async Task<AdminDashboardResponse> BuildDashboardAsync(CancellationToken cancellationToken)
    {
        var markets = await _marketRepository.GetAsync(cancellationToken: cancellationToken);
        var products = await _productRepository.GetAsync(cancellationToken: cancellationToken);
        var prices = await _priceRepository.GetAsync(cancellationToken: cancellationToken);
        var pendingMarkets = await _marketRepository.GetPendingAsync(cancellationToken);
        var pendingPrices = await _priceRepository.GetPendingAsync(cancellationToken);
        var visits = await _analyticsRepository.GetRecentVisitsAsync(cancellationToken);
        var today = GetBangladeshDayStartUtc(DateTime.UtcNow);
        var newContactMessages = await _contactRepository.CountAsync(status: "New", cancellationToken: cancellationToken);
        var inProgressContactMessages = await _contactRepository.CountAsync(status: "InProgress", cancellationToken: cancellationToken);

        return new AdminDashboardResponse
        {
            Traffic = new TrafficSummaryResponse
            {
                TotalVisits = await _analyticsRepository.GetTotalVisitsAsync(cancellationToken),
                UniqueVisitors = await _analyticsRepository.GetUniqueVisitorsAsync(cancellationToken),
                UniqueVisitorsToday = await _analyticsRepository.GetUniqueVisitorsSinceAsync(today, cancellationToken),
                TodayVisits = await _analyticsRepository.GetVisitCountSinceAsync(today, cancellationToken),
                ThisWeekVisits = await _analyticsRepository.GetVisitCountSinceAsync(today.AddDays(-7), cancellationToken),
                ThisMonthVisits = await _analyticsRepository.GetVisitCountSinceAsync(today.AddDays(-30), cancellationToken)
            },
            Records = new AdminMetricResponse
            {
                TotalMarkets = markets.Count,
                TotalProducts = products.Count,
                TotalCategories = products.Select(x => x.CategoryId).Distinct().Count(),
                TotalPriceSubmissions = prices.Count,
                TotalContributors = 0
            },
            Moderation = new ModerationQueueResponse
            {
                PendingMarkets = pendingMarkets.Count,
                PendingProducts = products.Count(x => x.Status.ToString() == "Pending"),
                PendingPriceSubmissions = pendingPrices.Count,
                FlaggedPriceSubmissions = prices.Count(x => x.Status.ToString() == "Flagged"),
                PendingContactMessages = newContactMessages + inProgressContactMessages
            },
            PeakHours = visits
                .GroupBy(x => x.VisitedAt.Hour)
                .Select(x => new PeakHourResponse { Hour = x.Key, VisitCount = x.Count() })
                .OrderByDescending(x => x.VisitCount)
                .ThenBy(x => x.Hour)
                .ToList()
        };
    }

    private static string FormatHourRange(int hour)
    {
        var normalizedHour = ((hour % 24) + 24) % 24;
        var nextHour = (normalizedHour + 1) % 24;

        return $"{FormatClockHour(normalizedHour)} - {FormatClockHour(nextHour)}";
    }

    private static string FormatClockHour(int hour)
    {
        if (hour == 0)
        {
            return "12 AM";
        }

        if (hour == 12)
        {
            return "12 PM";
        }

        return hour < 12 ? $"{hour} AM" : $"{hour - 12} PM";
    }

    private static DateTime GetBangladeshDayStartUtc(DateTime utcNow)
    {
        return (utcNow + BangladeshUtcOffset).Date - BangladeshUtcOffset;
    }
}
