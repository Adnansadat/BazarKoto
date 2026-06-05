using BazarKoto.Application.Features.Prices;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Interfaces;

public interface IPriceRepository
{
    Task<IReadOnlyList<PriceSubmission>> GetAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        SubmissionStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PriceSubmission> Items, int TotalCount)> GetPageAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        SubmissionStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceSubmission>> GetPublicProductPricesAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PriceSubmission> Items, int TotalCount)> GetPublicProductPricesPageAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PriceSubmission> Items, int TotalCount)> GetAdminPricesAsync(
        string? search = null,
        SubmissionStatus? status = null,
        DateTime? submittedFrom = null,
        DateTime? submittedTo = null,
        Guid? productId = null,
        Guid? marketId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceSubmission>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(SubmissionStatus? status = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PriceSubmission> Items, int TotalCount)> GetPendingPageAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HomePricePreviewItem>> GetHomePricePreviewAsync(
        int limit = 60,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyPriceSummary>> GetDailySummaryAggregatesAsync(
        DateOnly date,
        SubmissionStatus status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceSubmission>> GetTodayAsync(DateOnly date, SubmissionStatus? status = null, CancellationToken cancellationToken = default);
    Task<PriceSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(PriceSubmission priceSubmission, CancellationToken cancellationToken = default);
    void Update(PriceSubmission priceSubmission);
}
