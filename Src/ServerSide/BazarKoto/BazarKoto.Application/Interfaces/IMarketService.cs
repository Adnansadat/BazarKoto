using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Markets;

namespace BazarKoto.Application.Interfaces;

public interface IMarketService
{
    Task<PagedResponse<MarketResponse>> GetMarketsAsync(MarketSearchRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<MarketResponse>> GetNearbyMarketsAsync(MarketSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MarketResponse>> CreateMarketAsync(CreateMarketRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<MarketResponse>> GetPendingMarketsAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MarketResponse>> UpdateMarketAsync(Guid id, UpdateMarketRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteMarketAsync(Guid id, CancellationToken cancellationToken = default);
}
