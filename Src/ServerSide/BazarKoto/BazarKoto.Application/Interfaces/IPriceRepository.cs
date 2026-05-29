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

    Task<IReadOnlyList<PriceSubmission>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceSubmission>> GetTodayAsync(DateOnly date, SubmissionStatus? status = null, CancellationToken cancellationToken = default);
    Task<PriceSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(PriceSubmission priceSubmission, CancellationToken cancellationToken = default);
    void Update(PriceSubmission priceSubmission);
}
