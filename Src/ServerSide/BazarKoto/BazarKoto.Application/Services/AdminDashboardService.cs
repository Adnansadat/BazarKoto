using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Analytics;
using BazarKoto.Contracts.Admin;
using BazarKoto.Contracts.Common;
using BazarKoto.Domain.Enums;

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
        var today = GetBangladeshDayStartUtc(DateTime.UtcNow);
        var totalMarkets = await _marketRepository.CountAsync(cancellationToken: cancellationToken);
        var totalProducts = await _productRepository.CountAsync(cancellationToken: cancellationToken);
        var totalCategories = await _productRepository.CountDistinctCategoriesAsync(RecordStatus.Approved, cancellationToken);
        var totalPriceSubmissions = await _priceRepository.CountAsync(cancellationToken: cancellationToken);
        var pendingMarkets = await _marketRepository.CountByStatusAsync(RecordStatus.Pending, cancellationToken);
        var pendingProducts = await _productRepository.CountByStatusAsync(RecordStatus.Pending, cancellationToken);
        var pendingPrices = await _priceRepository.CountAsync(SubmissionStatus.Pending, cancellationToken);
        var flaggedPrices = await _priceRepository.CountAsync(SubmissionStatus.Flagged, cancellationToken);
        var newContactMessages = await _contactRepository.CountAsync(status: "New", cancellationToken: cancellationToken);
        var inProgressContactMessages = await _contactRepository.CountAsync(status: "InProgress", cancellationToken: cancellationToken);
        var peakHours = await _analyticsRepository.GetPeakHoursAsync(cancellationToken);

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
                TotalMarkets = totalMarkets,
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                TotalPriceSubmissions = totalPriceSubmissions,
                TotalContributors = 0
            },
            Moderation = new ModerationQueueResponse
            {
                PendingMarkets = pendingMarkets,
                PendingProducts = pendingProducts,
                PendingPriceSubmissions = pendingPrices,
                FlaggedPriceSubmissions = flaggedPrices,
                PendingContactMessages = newContactMessages + inProgressContactMessages
            },
            PeakHours = peakHours.ToList()
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
