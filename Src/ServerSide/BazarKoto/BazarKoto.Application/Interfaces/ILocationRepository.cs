using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Interfaces;

public interface ILocationRepository
{
    Task<IReadOnlyList<Division>> GetDivisionsAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<District>> GetDistrictsAsync(Guid divisionId, string? search = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Upazila>> GetUpazilasAsync(Guid districtId, string? search = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnionOrWard>> GetUnionOrWardsAsync(Guid upazilaId, string? search = null, CancellationToken cancellationToken = default);
}
