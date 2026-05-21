using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Locations;

namespace BazarKoto.Application.Interfaces;

public interface ILocationService
{
    Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetDivisionsAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetDistrictsAsync(Guid divisionId, string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetUpazilasAsync(Guid districtId, string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetUnionOrWardsAsync(Guid upazilaId, string? search = null, CancellationToken cancellationToken = default);
}
