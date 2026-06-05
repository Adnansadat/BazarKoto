using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Features.Prices;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Prices;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class PriceSummaryService : IPriceSummaryService
{
    private readonly IPriceRepository _priceRepository;
    private readonly IPriceSummaryRepository _priceSummaryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PriceSummaryService(
        IPriceRepository priceRepository,
        IPriceSummaryRepository priceSummaryRepository,
        IUnitOfWork unitOfWork)
    {
        _priceRepository = priceRepository;
        _priceSummaryRepository = priceSummaryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task RecalculateDailySummaryAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var summaries = await _priceRepository.GetDailySummaryAggregatesAsync(
            date,
            SubmissionStatus.Approved,
            cancellationToken);

        foreach (var calculatedSummary in summaries)
        {
            var summary = await _priceSummaryRepository.GetForUpsertAsync(
                calculatedSummary.ProductId,
                calculatedSummary.MarketId,
                calculatedSummary.DivisionId,
                calculatedSummary.DistrictId,
                calculatedSummary.UpazilaId,
                calculatedSummary.UnionOrWardId,
                calculatedSummary.PriceDate,
                cancellationToken);

            if (summary is null)
            {
                summary = new DailyPriceSummary
                {
                    ProductId = calculatedSummary.ProductId,
                    MarketId = calculatedSummary.MarketId,
                    DivisionId = calculatedSummary.DivisionId,
                    DistrictId = calculatedSummary.DistrictId,
                    UpazilaId = calculatedSummary.UpazilaId,
                    UnionOrWardId = calculatedSummary.UnionOrWardId,
                    PriceDate = calculatedSummary.PriceDate
                };

                await _priceSummaryRepository.AddAsync(summary, cancellationToken);
            }

            summary.MinPrice = calculatedSummary.MinPrice;
            summary.MaxPrice = calculatedSummary.MaxPrice;
            summary.AveragePrice = calculatedSummary.AveragePrice;
            summary.SubmissionCount = calculatedSummary.SubmissionCount;
            summary.LastUpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<PriceSubmissionResponse>> GetPriceSummariesAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        var (summaries, totalCount) = await GetSummariesPageAsync(request, request.Date, cancellationToken);
        return ToPagedResponse(summaries.Select(ToPriceResponse), totalCount, request);
    }

    public async Task<PagedResponse<PriceSubmissionResponse>> GetTodaySummaryAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (summaries, totalCount) = await GetSummariesPageAsync(request, today, cancellationToken);
        return ToPagedResponse(summaries.Select(ToPriceResponse), totalCount, request);
    }

    public async Task<ApiResponse<PriceSummaryResponse>> GetSummaryAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        var summary = await GetSummaryAggregateAsync(request, request.Date, cancellationToken);

        if (summary is null)
        {
            return ApiResponse<PriceSummaryResponse>.Ok(new PriceSummaryResponse(), "No price summary found.");
        }

        return ApiResponse<PriceSummaryResponse>.Ok(ToSummaryResponse(summary));
    }

    private Task<(IReadOnlyList<DailyPriceSummary> Items, int TotalCount)> GetSummariesPageAsync(
        PriceSearchRequest request,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        return _priceSummaryRepository.GetPageAsync(
            request.DivisionId,
            request.DistrictId,
            request.UpazilaId,
            request.UnionOrWardId,
            request.MarketId,
            request.CategoryId,
            request.ProductId,
            date,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private Task<PriceSummaryAggregate?> GetSummaryAggregateAsync(
        PriceSearchRequest request,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        return _priceSummaryRepository.GetAggregateAsync(
            request.DivisionId,
            request.DistrictId,
            request.UpazilaId,
            request.UnionOrWardId,
            request.MarketId,
            request.CategoryId,
            request.ProductId,
            date,
            cancellationToken);
    }

    private static PriceSubmissionResponse ToPriceResponse(DailyPriceSummary summary)
    {
        return new PriceSubmissionResponse
        {
            Id = summary.Id,
            MarketId = summary.MarketId ?? Guid.Empty,
            MarketName = summary.Market?.MarketName ?? string.Empty,
            ProductId = summary.ProductId,
            ProductNameEn = summary.Product?.NameEn ?? string.Empty,
            ProductNameBn = summary.Product?.NameBn ?? string.Empty,
            CategoryId = summary.Product?.CategoryId ?? Guid.Empty,
            CategoryNameEn = summary.Product?.Category?.NameEn ?? string.Empty,
            CategoryNameBn = summary.Product?.Category?.NameBn ?? string.Empty,
            Unit = summary.Product?.PrimaryUnit ?? string.Empty,
            PricePerUnit = summary.AveragePrice,
            QuantityChecked = null,
            PriceDate = summary.PriceDate,
            PriceTime = null,
            SellerType = string.Empty,
            PriceSource = "DailySummary",
            QualityGrade = string.Empty,
            Notes = null,
            Status = SubmissionStatus.Approved.ToString()
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

    private static PagedResponse<T> ToPagedResponse<T>(IEnumerable<T> items, int totalCount, PaginationRequest request)
    {
        return new PagedResponse<T>
        {
            Message = "Success",
            Data = items.ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    private static PriceSummaryResponse ToSummaryResponse(PriceSummaryAggregate summary)
    {
        return new PriceSummaryResponse
        {
            ProductId = summary.ProductId,
            ProductName = summary.ProductName,
            Unit = summary.Unit,
            MinimumPrice = summary.MinimumPrice,
            MaximumPrice = summary.MaximumPrice,
            AveragePrice = summary.SubmissionCount == 0 ? 0 : summary.WeightedPriceTotal / summary.SubmissionCount,
            SubmissionCount = summary.SubmissionCount,
            FromDate = summary.FromDate,
            ToDate = summary.ToDate
        };
    }
}
