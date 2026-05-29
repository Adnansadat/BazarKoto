using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Markets;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class MarketService : IMarketService
{
    private readonly IMarketRepository _marketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarketService(IMarketRepository marketRepository, IUnitOfWork unitOfWork)
    {
        _marketRepository = marketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<MarketResponse>> GetMarketsAsync(MarketSearchRequest request, CancellationToken cancellationToken = default)
    {
        var markets = await _marketRepository.GetAsync(request.DivisionId, request.DistrictId, request.UpazilaId, request.UnionOrWardId, request.Search, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _marketRepository.CountAsync(request.DivisionId, request.DistrictId, request.UpazilaId, request.UnionOrWardId, request.Search, cancellationToken);
        return Page(markets.Select(ToResponse), request, totalCount);
    }

    public async Task<PagedResponse<MarketOptionResponse>> GetMarketOptionsAsync(MarketSearchRequest request, CancellationToken cancellationToken = default)
    {
        var markets = await _marketRepository.GetOptionsAsync(request.DivisionId, request.DistrictId, request.UpazilaId, request.UnionOrWardId, request.Search, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _marketRepository.CountOptionsAsync(request.DivisionId, request.DistrictId, request.UpazilaId, request.UnionOrWardId, request.Search, cancellationToken);
        return Page(markets.Select(ToOptionResponse), request, totalCount);
    }

    public async Task<PagedResponse<MarketResponse>> GetNearbyMarketsAsync(MarketSearchRequest request, CancellationToken cancellationToken = default)
    {
        var markets = await _marketRepository.GetAsync(request.DivisionId, request.DistrictId, request.UpazilaId, request.UnionOrWardId, request.Search, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _marketRepository.CountAsync(request.DivisionId, request.DistrictId, request.UpazilaId, request.UnionOrWardId, request.Search, cancellationToken);
        return Page(markets.Select(ToResponse), request, totalCount);
    }

    public async Task<ApiResponse<MarketResponse>> CreateMarketAsync(CreateMarketRequest request, CancellationToken cancellationToken = default)
    {
        var duplicateMarket = await _marketRepository.FindDuplicateAsync(
            request.DivisionId,
            request.DistrictId,
            request.UpazilaId,
            request.UnionOrWardId,
            request.Area,
            request.MarketName,
            cancellationToken);

        if (duplicateMarket is not null)
        {
            return ApiResponse<MarketResponse>.Ok(ToResponse(duplicateMarket), "This market already exists for the selected location.");
        }

        var market = new Market
        {
            DivisionId = request.DivisionId,
            DistrictId = request.DistrictId,
            UpazilaId = request.UpazilaId,
            UnionOrWardId = request.UnionOrWardId,
            Area = (request.Area ?? string.Empty).Trim(),
            MarketName = (request.MarketName ?? string.Empty).Trim(),
            VillageOrMoholla = (request.VillageOrMoholla ?? string.Empty).Trim(),
            Landmark = (request.Landmark ?? string.Empty).Trim(),
            Notes = request.Notes?.Trim(),
            MarketType = ParseEnum(request.MarketType, MarketType.Retail),
            OperatingSchedule = ParseEnum(request.OperatingSchedule, OperatingSchedule.Daily),
            Status = RecordStatus.Pending
        };

        await _marketRepository.AddAsync(market, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<MarketResponse>.Ok(ToResponse(market), "Market submitted successfully.");
    }

    public async Task<PagedResponse<MarketResponse>> GetPendingMarketsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var markets = await _marketRepository.GetPendingAsync(cancellationToken);
        return Page(markets.Select(ToResponse), request);
    }

    public Task<ApiResponse<MarketResponse>> UpdateMarketAsync(Guid id, UpdateMarketRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponse<MarketResponse>.Fail("Market persistence is not configured yet."));
    }

    public Task<ApiResponse<object>> DeleteMarketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponse<object>.Fail("Market persistence is not configured yet."));
    }

    private static MarketResponse ToResponse(Market market)
    {
        return new MarketResponse
        {
            Id = market.Id,
            DivisionId = market.DivisionId,
            DivisionNameEn = market.Division?.NameEn ?? string.Empty,
            DivisionNameBn = market.Division?.NameBn ?? string.Empty,
            DistrictId = market.DistrictId,
            DistrictNameEn = market.District?.NameEn ?? string.Empty,
            DistrictNameBn = market.District?.NameBn ?? string.Empty,
            UpazilaId = market.UpazilaId,
            UpazilaNameEn = market.Upazila?.NameEn ?? string.Empty,
            UpazilaNameBn = market.Upazila?.NameBn ?? string.Empty,
            UnionOrWardId = market.UnionOrWardId,
            UnionOrWardNameEn = market.UnionOrWard?.NameEn,
            UnionOrWardNameBn = market.UnionOrWard?.NameBn,
            Area = market.Area,
            MarketName = market.MarketName,
            VillageOrMoholla = market.VillageOrMoholla,
            Landmark = market.Landmark,
            Notes = market.Notes,
            MarketType = market.MarketType.ToString(),
            OperatingSchedule = market.OperatingSchedule.ToString(),
            Status = market.Status.ToString(),
            CreatedAt = market.CreatedAt,
            UpdatedAt = market.UpdatedAt
        };
    }

    private static MarketOptionResponse ToOptionResponse(Market market)
    {
        return new MarketOptionResponse
        {
            Id = market.Id,
            MarketId = market.Id,
            MarketName = market.MarketName,
            DisplayLabel = BuildMarketDisplayLabel(market),
            DivisionId = market.DivisionId,
            DivisionNameEn = market.Division?.NameEn ?? string.Empty,
            DivisionNameBn = market.Division?.NameBn ?? string.Empty,
            DistrictId = market.DistrictId,
            DistrictNameEn = market.District?.NameEn ?? string.Empty,
            DistrictNameBn = market.District?.NameBn ?? string.Empty,
            UpazilaId = market.UpazilaId,
            UpazilaNameEn = market.Upazila?.NameEn ?? string.Empty,
            UpazilaNameBn = market.Upazila?.NameBn ?? string.Empty,
            UnionOrWardId = market.UnionOrWardId,
            UnionOrWardNameEn = market.UnionOrWard?.NameEn,
            UnionOrWardNameBn = market.UnionOrWard?.NameBn
        };
    }

    private static string BuildMarketDisplayLabel(Market market)
    {
        var locationParts = new[]
        {
            market.UnionOrWard?.NameEn,
            market.Upazila?.NameEn,
            market.District?.NameEn,
            string.IsNullOrWhiteSpace(market.Division?.NameEn) ? null : $"{market.Division.NameEn} Division"
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        var location = string.Join(", ", locationParts);
        return string.IsNullOrWhiteSpace(location) ? market.MarketName : $"{market.MarketName} — {location}";
    }

    private static PagedResponse<T> Page<T>(IEnumerable<T> items, PaginationRequest request, int? totalCount = null)
    {
        var list = items.ToList();
        var count = totalCount ?? list.Count;
        var pageItems = totalCount.HasValue ? list : list.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();

        return new PagedResponse<T>
        {
            Message = "Success",
            Data = pageItems,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)request.PageSize)
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }
}
