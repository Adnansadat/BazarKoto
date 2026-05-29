using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Prices;

namespace BazarKoto.Application.Interfaces;

public interface IPriceService
{
    Task<PagedResponse<PriceSubmissionResponse>> GetPricesAsync(PriceSearchRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<PublicProductPriceResponse>> GetPublicProductPricesAsync(PublicProductPriceSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PriceSubmissionResponse>> GetLatestPriceAsync(PriceSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PriceSubmissionResponse>> SubmitPriceAsync(SubmitPriceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PriceSubmissionResponse>> UpdatePriceAsync(Guid id, UpdatePriceRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<PriceSubmissionResponse>> GetTodayPricesAsync(PriceSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PriceSummaryResponse>> GetPriceSummaryAsync(PriceSearchRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<PriceSubmissionResponse>> GetPendingPricesAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PriceSubmissionResponse>> ApprovePriceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PriceSubmissionResponse>> RejectPriceAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default);
}
