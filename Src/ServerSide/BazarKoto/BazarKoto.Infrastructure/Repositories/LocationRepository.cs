using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly BazarKotoDbContext _dbContext;

    public LocationRepository(BazarKotoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Division>> GetDivisionsAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Divisions.AsNoTracking().Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x => x.NameEn.Contains(normalizedSearch) || x.NameBn.Contains(normalizedSearch));
        }

        return await query.OrderBy(x => x.NameEn).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<District>> GetDistrictsAsync(Guid divisionId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Districts.AsNoTracking().Where(x => x.IsActive && x.DivisionId == divisionId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x => x.NameEn.Contains(normalizedSearch) || x.NameBn.Contains(normalizedSearch));
        }

        return await query.OrderBy(x => x.NameEn).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Upazila>> GetUpazilasAsync(Guid districtId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Upazilas.AsNoTracking().Where(x => x.IsActive && x.DistrictId == districtId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x => x.NameEn.Contains(normalizedSearch) || x.NameBn.Contains(normalizedSearch));
        }

        return await query.OrderBy(x => x.NameEn).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnionOrWard>> GetUnionOrWardsAsync(Guid upazilaId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UnionOrWards.AsNoTracking().Where(x => x.IsActive && x.UpazilaId == upazilaId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x => x.NameEn.Contains(normalizedSearch) || x.NameBn.Contains(normalizedSearch));
        }

        return await query.OrderBy(x => x.NameEn).ToListAsync(cancellationToken);
    }
}
