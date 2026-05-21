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
