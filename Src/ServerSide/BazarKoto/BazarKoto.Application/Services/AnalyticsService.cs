using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Analytics;
using BazarKoto.Contracts.Common;
using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AnalyticsService(IAnalyticsRepository analyticsRepository, IUnitOfWork unitOfWork)
    {
        _analyticsRepository = analyticsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<object>> TrackPageVisitAsync(TrackPageVisitRequest request, CancellationToken cancellationToken = default)
    {
        await _analyticsRepository.AddPageVisitAsync(new PageVisit
        {
            Path = request.Path,
            PageTitle = request.PageTitle,
            VisitorId = request.VisitorId,
            Referrer = request.Referrer,
            DeviceType = request.DeviceType,
            Country = request.Country ?? string.Empty,
            UserAgent = string.Empty,
            IpHash = string.Empty,
            VisitedAt = DateTime.UtcNow
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.Ok(new object(), "Page visit accepted.");
    }

    public async Task<ApiResponse<TrafficSummaryResponse>> GetTrafficSummaryAsync(CancellationToken cancellationToken = default)
    {
        var visits = await _analyticsRepository.GetRecentVisitsAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        return ApiResponse<TrafficSummaryResponse>.Ok(new TrafficSummaryResponse
        {
            TotalVisits = await _analyticsRepository.GetTotalVisitsAsync(cancellationToken),
            UniqueVisitors = await _analyticsRepository.GetUniqueVisitorsAsync(cancellationToken),
            TodayVisits = visits.Count(x => x.VisitedAt.Date == today),
            ThisWeekVisits = visits.Count(x => x.VisitedAt >= today.AddDays(-7)),
            ThisMonthVisits = visits.Count(x => x.VisitedAt >= today.AddDays(-30))
        });
    }

    public async Task<ApiResponse<IReadOnlyList<PeakHourResponse>>> GetPeakHoursAsync(CancellationToken cancellationToken = default)
    {
        var visits = await _analyticsRepository.GetRecentVisitsAsync(cancellationToken);
        var peakHours = visits
            .GroupBy(x => x.VisitedAt.Hour)
            .Select(x => new PeakHourResponse { Hour = x.Key, VisitCount = x.Count() })
            .OrderByDescending(x => x.VisitCount)
            .ThenBy(x => x.Hour)
            .ToList();

        return ApiResponse<IReadOnlyList<PeakHourResponse>>.Ok(peakHours);
    }

    public async Task<ApiResponse<AdReadinessResponse>> GetAdReadinessAsync(CancellationToken cancellationToken = default)
    {
        var totalVisits = await _analyticsRepository.GetTotalVisitsAsync(cancellationToken);
        var uniqueVisitors = await _analyticsRepository.GetUniqueVisitorsAsync(cancellationToken);

        return ApiResponse<AdReadinessResponse>.Ok(new AdReadinessResponse
        {
            IsReady = totalVisits > 0,
            TotalVisits = totalVisits,
            UniqueVisitors = uniqueVisitors,
            ClickThroughRate = 0,
            Message = totalVisits > 0 ? "Traffic data is available." : "Not enough traffic data yet."
        });
    }
}
