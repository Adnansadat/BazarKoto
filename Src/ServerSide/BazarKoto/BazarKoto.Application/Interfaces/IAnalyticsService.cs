using BazarKoto.Contracts.Analytics;
using BazarKoto.Contracts.Common;

namespace BazarKoto.Application.Interfaces;

public interface IAnalyticsService
{
    Task<ApiResponse<object>> TrackPageVisitAsync(TrackPageVisitRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TrafficSummaryResponse>> GetTrafficSummaryAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<PeakHourResponse>>> GetPeakHoursAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<AdReadinessResponse>> GetAdReadinessAsync(CancellationToken cancellationToken = default);
}
