using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface IMarketRepository
{
    Task<IReadOnlyList<Market>> GetAsync(Guid? divisionId = null, Guid? districtId = null, Guid? upazilaId = null, Guid? unionOrWardId = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid? divisionId = null, Guid? districtId = null, Guid? upazilaId = null, Guid? unionOrWardId = null, string? search = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid divisionId, Guid districtId, Guid upazilaId, Guid? unionOrWardId, string area, string marketName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Market>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<Market?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Market market, CancellationToken cancellationToken = default);
    void Update(Market market);
    void Delete(Market market);
}
