using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Analytics;
using BazarKoto.Contracts.Common;
using BazarKoto.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace BazarKoto.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private static readonly TimeSpan BangladeshUtcOffset = TimeSpan.FromHours(6);
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IUserTrackingRequestContextAccessor _requestContextAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        IUserTrackingRequestContextAccessor requestContextAccessor,
        IUnitOfWork unitOfWork)
    {
        _analyticsRepository = analyticsRepository;
        _requestContextAccessor = requestContextAccessor;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<object>> TrackPageVisitAsync(TrackPageVisitRequest request, CancellationToken cancellationToken = default)
    {
        var userAgent = NormalizeUserAgent(_requestContextAccessor.RawUserAgent);
        var ipHash = HashOptional(_requestContextAccessor.RawIpAddress);
        var visitorId = NormalizeVisitorId(request.VisitorId) ?? BuildFallbackVisitorId(ipHash, userAgent);

        await _analyticsRepository.AddPageVisitAsync(new PageVisit
        {
            Path = NormalizeRequired(request.Path, 512),
            PageTitle = NormalizeOptional(request.PageTitle, 256) ?? "Untitled page",
            VisitorId = visitorId,
            Referrer = NormalizeOptional(request.Referrer, 512),
            DeviceType = NormalizeOptional(request.DeviceType, 32) ?? "Unknown",
            Country = request.Country ?? string.Empty,
            UserAgent = userAgent ?? string.Empty,
            IpHash = ipHash ?? string.Empty,
            VisitedAt = DateTime.UtcNow
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.Ok(new object(), "Page visit accepted.");
    }

    public async Task<ApiResponse<TrafficSummaryResponse>> GetTrafficSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = GetBangladeshDayStartUtc(DateTime.UtcNow);

        return ApiResponse<TrafficSummaryResponse>.Ok(new TrafficSummaryResponse
        {
            TotalVisits = await _analyticsRepository.GetTotalVisitsAsync(cancellationToken),
            UniqueVisitors = await _analyticsRepository.GetUniqueVisitorsAsync(cancellationToken),
            UniqueVisitorsToday = await _analyticsRepository.GetUniqueVisitorsSinceAsync(today, cancellationToken),
            TodayVisits = await _analyticsRepository.GetVisitCountSinceAsync(today, cancellationToken),
            ThisWeekVisits = await _analyticsRepository.GetVisitCountSinceAsync(today.AddDays(-7), cancellationToken),
            ThisMonthVisits = await _analyticsRepository.GetVisitCountSinceAsync(today.AddDays(-30), cancellationToken)
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

    private static string? NormalizeVisitorId(string? visitorId)
    {
        var normalized = NormalizeOptional(visitorId, 128);
        return Guid.TryParse(normalized, out var parsed) && parsed != Guid.Empty ? parsed.ToString("D") : null;
    }

    private static string BuildFallbackVisitorId(string? ipHash, string? userAgent)
    {
        var fallbackSource = $"{ipHash ?? "unknown-ip"}|{userAgent ?? "unknown-agent"}";
        return $"server-{Hash(fallbackSource)[..32]}";
    }

    private static string NormalizeRequired(string? value, int maxLength)
    {
        return NormalizeOptional(value, maxLength) ?? "/";
    }

    private static string? NormalizeUserAgent(string? value)
    {
        return NormalizeOptional(value, 512);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? HashOptional(string? value)
    {
        var normalized = NormalizeOptional(value, 128);
        return normalized is null ? null : Hash(normalized.ToLowerInvariant());
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static DateTime GetBangladeshDayStartUtc(DateTime utcNow)
    {
        return (utcNow + BangladeshUtcOffset).Date - BangladeshUtcOffset;
    }
}
