using BazarKoto.Application.Interfaces;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using BazarKoto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazarKoto.Infrastructure.Repositories;

public class MarketRepository : IMarketRepository
{
    private readonly BazarKotoDbContext _dbContext;

    public MarketRepository(BazarKotoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Market>> GetAsync(Guid? divisionId = null, Guid? districtId = null, Guid? upazilaId = null, Guid? unionOrWardId = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(divisionId, districtId, upazilaId, unionOrWardId, search);

        return await query
            .OrderBy(x => x.MarketName)
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(Guid? divisionId = null, Guid? districtId = null, Guid? upazilaId = null, Guid? unionOrWardId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        return BuildQuery(divisionId, districtId, upazilaId, unionOrWardId, search).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Market>> GetOptionsAsync(Guid? divisionId = null, Guid? districtId = null, Guid? upazilaId = null, Guid? unionOrWardId = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = BuildOptionQuery(divisionId, districtId, upazilaId, unionOrWardId, search);

        return await query
            .OrderBy(x => x.MarketName)
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountOptionsAsync(Guid? divisionId = null, Guid? districtId = null, Guid? upazilaId = null, Guid? unionOrWardId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        return BuildOptionQuery(divisionId, districtId, upazilaId, unionOrWardId, search).CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid divisionId, Guid districtId, Guid upazilaId, Guid? unionOrWardId, string area, string marketName, CancellationToken cancellationToken = default)
    {
        return await FindDuplicateAsync(divisionId, districtId, upazilaId, unionOrWardId, area, marketName, cancellationToken) is not null;
    }

    public async Task<Market?> FindDuplicateAsync(Guid divisionId, Guid districtId, Guid upazilaId, Guid? unionOrWardId, string area, string marketName, CancellationToken cancellationToken = default)
    {
        var normalizedMarketName = NormalizeComparableText(marketName);

        var locationMarkets = await Query().AsNoTracking()
            .Where(x =>
                x.DivisionId == divisionId &&
                x.DistrictId == districtId &&
                x.UpazilaId == upazilaId &&
                x.UnionOrWardId == unionOrWardId)
            .ToListAsync(cancellationToken);

        return locationMarkets.FirstOrDefault(existingMarket =>
            AreComparableTextsSimilar(NormalizeComparableText(existingMarket.MarketName), normalizedMarketName));
    }

    public async Task<IReadOnlyList<Market>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await Query().AsNoTracking().Where(x => x.Status == RecordStatus.Pending).ToListAsync(cancellationToken);
    }

    public Task<Market?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Market market, CancellationToken cancellationToken = default)
    {
        await _dbContext.Markets.AddAsync(market, cancellationToken);
    }

    public void Update(Market market)
    {
        _dbContext.Markets.Update(market);
    }

    public void Delete(Market market)
    {
        _dbContext.Markets.Remove(market);
    }

    private IQueryable<Market> Query()
    {
        return _dbContext.Markets
            .Include(x => x.Division)
            .Include(x => x.District)
            .Include(x => x.Upazila)
            .Include(x => x.UnionOrWard);
    }

    private IQueryable<Market> BuildQuery(Guid? divisionId, Guid? districtId, Guid? upazilaId, Guid? unionOrWardId, string? search)
    {
        var query = Query().AsNoTracking().Where(x => x.Status == RecordStatus.Approved);

        return ApplySearchAndLocationFilters(query, divisionId, districtId, upazilaId, unionOrWardId, search);
    }

    private IQueryable<Market> BuildOptionQuery(Guid? divisionId, Guid? districtId, Guid? upazilaId, Guid? unionOrWardId, string? search)
    {
        var query = Query().AsNoTracking().Where(x => x.Status == RecordStatus.Approved || x.Status == RecordStatus.Pending);

        return ApplySearchAndLocationFilters(query, divisionId, districtId, upazilaId, unionOrWardId, search);
    }

    private static IQueryable<Market> ApplySearchAndLocationFilters(IQueryable<Market> query, Guid? divisionId, Guid? districtId, Guid? upazilaId, Guid? unionOrWardId, string? search)
    {
        if (divisionId.HasValue)
        {
            query = query.Where(x => x.DivisionId == divisionId.Value);
        }

        if (districtId.HasValue)
        {
            query = query.Where(x => x.DistrictId == districtId.Value);
        }

        if (upazilaId.HasValue)
        {
            query = query.Where(x => x.UpazilaId == upazilaId.Value);
        }

        if (unionOrWardId.HasValue)
        {
            query = query.Where(x => x.UnionOrWardId == unionOrWardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x =>
                x.MarketName.Contains(normalizedSearch) ||
                x.Area.Contains(normalizedSearch) ||
                x.VillageOrMoholla.Contains(normalizedSearch) ||
                x.Landmark.Contains(normalizedSearch));
        }

        return query;
    }

    private static string NormalizeComparableText(string value)
    {
        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static bool AreComparableTextsSimilar(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        if (left == right || left.Contains(right) || right.Contains(left))
        {
            return true;
        }

        return GetEditDistance(left, right) <= GetAllowedEditDistance(left, right);
    }

    private static int GetAllowedEditDistance(string left, string right)
    {
        var maxLength = Math.Max(left.Length, right.Length);

        if (maxLength <= 5)
        {
            return 1;
        }

        if (maxLength <= 10)
        {
            return 2;
        }

        return 3;
    }

    private static int GetEditDistance(string left, string right)
    {
        var previousRow = Enumerable.Range(0, right.Length + 1).ToArray();

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            var currentRow = new int[right.Length + 1];
            currentRow[0] = leftIndex;

            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var substitutionCost = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                currentRow[rightIndex] = Math.Min(
                    Math.Min(currentRow[rightIndex - 1] + 1, previousRow[rightIndex] + 1),
                    previousRow[rightIndex - 1] + substitutionCost);
            }

            previousRow = currentRow;
        }

        return previousRow[right.Length];
    }
}
