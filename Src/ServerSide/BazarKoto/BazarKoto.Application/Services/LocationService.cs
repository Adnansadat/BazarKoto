using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Locations;
using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;

    public LocationService(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetDivisionsAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var divisions = await _locationRepository.GetDivisionsAsync(search, cancellationToken);
        return ApiResponse<IReadOnlyList<LocationResponse>>.Ok(divisions.Select(ToResponse).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetDistrictsAsync(Guid divisionId, string? search = null, CancellationToken cancellationToken = default)
    {
        var districts = await _locationRepository.GetDistrictsAsync(divisionId, search, cancellationToken);
        return ApiResponse<IReadOnlyList<LocationResponse>>.Ok(districts.Select(ToResponse).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetUpazilasAsync(Guid districtId, string? search = null, CancellationToken cancellationToken = default)
    {
        var upazilas = await _locationRepository.GetUpazilasAsync(districtId, search, cancellationToken);
        return ApiResponse<IReadOnlyList<LocationResponse>>.Ok(upazilas.Select(ToResponse).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<LocationResponse>>> GetUnionOrWardsAsync(Guid upazilaId, string? search = null, CancellationToken cancellationToken = default)
    {
        var unionOrWards = await _locationRepository.GetUnionOrWardsAsync(upazilaId, search, cancellationToken);
        return ApiResponse<IReadOnlyList<LocationResponse>>.Ok(unionOrWards.Select(ToResponse).ToList());
    }

    private static LocationResponse ToResponse(Division division)
    {
        return new LocationResponse
        {
            Id = division.Id,
            NameEn = division.NameEn,
            NameBn = division.NameBn,
            Slug = division.Slug,
            BbsCode = division.BbsCode
        };
    }

    private static LocationResponse ToResponse(District district)
    {
        return new LocationResponse
        {
            Id = district.Id,
            NameEn = district.NameEn,
            NameBn = district.NameBn,
            Slug = district.Slug,
            BbsCode = district.BbsCode
        };
    }

    private static LocationResponse ToResponse(Upazila upazila)
    {
        return new LocationResponse
        {
            Id = upazila.Id,
            NameEn = upazila.NameEn,
            NameBn = upazila.NameBn,
            Slug = upazila.Slug,
            BbsCode = upazila.BbsCode
        };
    }

    private static LocationResponse ToResponse(UnionOrWard unionOrWard)
    {
        return new LocationResponse
        {
            Id = unionOrWard.Id,
            NameEn = unionOrWard.NameEn,
            NameBn = unionOrWard.NameBn,
            Slug = unionOrWard.Slug,
            BbsCode = unionOrWard.BbsCode,
            Type = unionOrWard.Type.ToString()
        };
    }
}
