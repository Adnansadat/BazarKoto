using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Prices;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class PriceService : IPriceService
{
    private readonly IPriceRepository _priceRepository;
    private readonly IPriceSummaryService _priceSummaryService;
    private readonly IUnitOfWork _unitOfWork;

    public PriceService(IPriceRepository priceRepository, IPriceSummaryService priceSummaryService, IUnitOfWork unitOfWork)
    {
        _priceRepository = priceRepository;
        _priceSummaryService = priceSummaryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<PriceSubmissionResponse>> GetPricesAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        return await _priceSummaryService.GetPriceSummariesAsync(request, cancellationToken);
    }

    public async Task<ApiResponse<PriceSubmissionResponse>> SubmitPriceAsync(SubmitPriceRequest request, CancellationToken cancellationToken = default)
    {
        var priceSubmission = new PriceSubmission
        {
            MarketId = request.MarketId,
            ProductId = request.ProductId,
            Unit = request.Unit,
            PricePerUnit = request.PricePerUnit,
            QuantityChecked = request.QuantityChecked,
            PriceDate = request.PriceDate,
            PriceTime = request.PriceTime,
            SellerType = ParseEnum(request.SellerType, SellerType.Retail),
            PriceSource = ParseEnum(request.PriceSource, PriceSource.UserReported),
            QualityGrade = ParseEnum(request.QualityGrade, QualityGrade.Standard),
            Notes = request.Notes,
            Status = SubmissionStatus.Pending
        };

        await _priceRepository.AddAsync(priceSubmission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PriceSubmissionResponse>.Ok(ToResponse(priceSubmission), "Price submitted successfully.");
    }

    public async Task<PagedResponse<PriceSubmissionResponse>> GetTodayPricesAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        return await _priceSummaryService.GetTodaySummaryAsync(request, cancellationToken);
    }

    public async Task<ApiResponse<PriceSummaryResponse>> GetPriceSummaryAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        return await _priceSummaryService.GetSummaryAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<PriceSubmissionResponse>> GetPendingPricesAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var prices = await _priceRepository.GetPendingAsync(cancellationToken);
        return Page(prices.Select(ToResponse), request);
    }

    public Task<ApiResponse<PriceSubmissionResponse>> ApprovePriceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponse<PriceSubmissionResponse>.Fail("Price persistence is not configured yet."));
    }

    public Task<ApiResponse<PriceSubmissionResponse>> RejectPriceAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResponse<PriceSubmissionResponse>.Fail("Price persistence is not configured yet."));
    }

    private static PriceSubmissionResponse ToResponse(PriceSubmission price)
    {
        return new PriceSubmissionResponse
        {
            Id = price.Id,
            MarketId = price.MarketId,
            MarketName = price.Market?.MarketName ?? string.Empty,
            ProductId = price.ProductId,
            ProductNameEn = price.Product?.NameEn ?? string.Empty,
            ProductNameBn = price.Product?.NameBn ?? string.Empty,
            CategoryId = price.Product?.CategoryId ?? Guid.Empty,
            CategoryNameEn = price.Product?.Category?.NameEn ?? string.Empty,
            CategoryNameBn = price.Product?.Category?.NameBn ?? string.Empty,
            Unit = price.Unit,
            PricePerUnit = price.PricePerUnit,
            QuantityChecked = price.QuantityChecked,
            PriceDate = price.PriceDate,
            PriceTime = price.PriceTime,
            SellerType = price.SellerType.ToString(),
            PriceSource = price.PriceSource.ToString(),
            QualityGrade = price.QualityGrade.ToString(),
            Notes = price.Notes,
            Status = price.Status.ToString(),
        };
    }

    private static PagedResponse<T> Page<T>(IEnumerable<T> items, PaginationRequest request)
    {
        var list = items.ToList();
        var pageItems = list.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();

        return new PagedResponse<T>
        {
            Message = "Success",
            Data = pageItems,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = list.Count,
            TotalPages = (int)Math.Ceiling(list.Count / (double)request.PageSize)
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }
}
