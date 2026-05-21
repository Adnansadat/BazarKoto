using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Prices;

namespace BazarKoto.Application.Interfaces;

public interface IPriceSummaryService
{
    Task RecalculateDailySummaryAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<PagedResponse<PriceSubmissionResponse>> GetPriceSummariesAsync(PriceSearchRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<PriceSubmissionResponse>> GetTodaySummaryAsync(PriceSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PriceSummaryResponse>> GetSummaryAsync(PriceSearchRequest request, CancellationToken cancellationToken = default);
}
