using BazarKoto.Application.Features.Prices;
using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface IPriceSummaryRepository
{
    Task<IReadOnlyList<DailyPriceSummary>> GetAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<DailyPriceSummary> Items, int TotalCount)> GetPageAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<PriceSummaryAggregate?> GetAggregateAsync(
        Guid? divisionId = null,
        Guid? districtId = null,
        Guid? upazilaId = null,
        Guid? unionOrWardId = null,
        Guid? marketId = null,
        Guid? categoryId = null,
        Guid? productId = null,
        DateOnly? date = null,
        CancellationToken cancellationToken = default);

    Task<DailyPriceSummary?> GetForUpsertAsync(
        Guid productId,
        Guid? marketId,
        Guid divisionId,
        Guid districtId,
        Guid upazilaId,
        Guid? unionOrWardId,
        DateOnly priceDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(DailyPriceSummary summary, CancellationToken cancellationToken = default);
}
