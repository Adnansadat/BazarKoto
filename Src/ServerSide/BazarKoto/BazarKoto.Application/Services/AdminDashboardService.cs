using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Analytics;
using BazarKoto.Contracts.Admin;
using BazarKoto.Contracts.Common;

namespace BazarKoto.Application.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IMarketRepository _marketRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly IAnalyticsRepository _analyticsRepository;

    public AdminDashboardService(
        IMarketRepository marketRepository,
        IProductRepository productRepository,
        IPriceRepository priceRepository,
        IAnalyticsRepository analyticsRepository)
    {
        _marketRepository = marketRepository;
        _productRepository = productRepository;
        _priceRepository = priceRepository;
        _analyticsRepository = analyticsRepository;
    }

    public async Task<ApiResponse<AdminDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var markets = await _marketRepository.GetAsync(cancellationToken: cancellationToken);
        var products = await _productRepository.GetAsync(cancellationToken: cancellationToken);
        var prices = await _priceRepository.GetAsync(cancellationToken: cancellationToken);
        var pendingMarkets = await _marketRepository.GetPendingAsync(cancellationToken);
        var pendingPrices = await _priceRepository.GetPendingAsync(cancellationToken);
        var visits = await _analyticsRepository.GetRecentVisitsAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        return ApiResponse<AdminDashboardResponse>.Ok(new AdminDashboardResponse
        {
            Traffic = new TrafficSummaryResponse
            {
                TotalVisits = await _analyticsRepository.GetTotalVisitsAsync(cancellationToken),
                UniqueVisitors = await _analyticsRepository.GetUniqueVisitorsAsync(cancellationToken),
                TodayVisits = visits.Count(x => x.VisitedAt.Date == today),
                ThisWeekVisits = visits.Count(x => x.VisitedAt >= today.AddDays(-7)),
                ThisMonthVisits = visits.Count(x => x.VisitedAt >= today.AddDays(-30))
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
                PendingContactMessages = 0
            },
            PeakHours = visits
                .GroupBy(x => x.VisitedAt.Hour)
                .Select(x => new PeakHourResponse { Hour = x.Key, VisitCount = x.Count() })
                .OrderByDescending(x => x.VisitCount)
                .ThenBy(x => x.Hour)
                .ToList()
        });
    }
}
