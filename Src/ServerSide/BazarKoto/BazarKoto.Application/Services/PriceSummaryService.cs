using BazarKoto.Application.Interfaces;
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
        var approvedPrices = await _priceRepository.GetAsync(
            date: date,
            status: SubmissionStatus.Approved,
            cancellationToken: cancellationToken);

        var groupedPrices = approvedPrices
            .Where(x => x.Market is not null)
            .GroupBy(x => new
            {
                x.ProductId,
                x.MarketId,
                x.Market!.DivisionId,
                x.Market.DistrictId,
                x.Market.UpazilaId,
                x.Market.UnionOrWardId,
                x.PriceDate
            });

        foreach (var group in groupedPrices)
        {
            var summary = await _priceSummaryRepository.GetForUpsertAsync(
                group.Key.ProductId,
                group.Key.MarketId,
                group.Key.DivisionId,
                group.Key.DistrictId,
                group.Key.UpazilaId,
                group.Key.UnionOrWardId,
                group.Key.PriceDate,
                cancellationToken);

            if (summary is null)
            {
                summary = new DailyPriceSummary
                {
                    ProductId = group.Key.ProductId,
                    MarketId = group.Key.MarketId,
                    DivisionId = group.Key.DivisionId,
                    DistrictId = group.Key.DistrictId,
                    UpazilaId = group.Key.UpazilaId,
                    UnionOrWardId = group.Key.UnionOrWardId,
                    PriceDate = group.Key.PriceDate
                };

                await _priceSummaryRepository.AddAsync(summary, cancellationToken);
            }

            summary.MinPrice = group.Min(x => x.PricePerUnit);
            summary.MaxPrice = group.Max(x => x.PricePerUnit);
            summary.AveragePrice = group.Average(x => x.PricePerUnit);
            summary.SubmissionCount = group.Count();
            summary.LastUpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<PriceSubmissionResponse>> GetPriceSummariesAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        var summaries = await GetSummariesAsync(request, request.Date, cancellationToken);
        return Page(summaries.Select(ToPriceResponse), request);
    }

    public async Task<PagedResponse<PriceSubmissionResponse>> GetTodaySummaryAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summaries = await GetSummariesAsync(request, today, cancellationToken);
        return Page(summaries.Select(ToPriceResponse), request);
    }

    public async Task<ApiResponse<PriceSummaryResponse>> GetSummaryAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        var summaries = await GetSummariesAsync(request, request.Date, cancellationToken);
        var first = summaries.FirstOrDefault();

        if (first is null)
        {
            return ApiResponse<PriceSummaryResponse>.Ok(new PriceSummaryResponse(), "No price summary found.");
        }

        var submissionCount = summaries.Sum(x => x.SubmissionCount);
        var weightedTotal = summaries.Sum(x => x.AveragePrice * x.SubmissionCount);

        return ApiResponse<PriceSummaryResponse>.Ok(new PriceSummaryResponse
        {
            ProductId = first.ProductId,
            ProductName = first.Product?.NameEn ?? string.Empty,
            Unit = first.Product?.PrimaryUnit ?? string.Empty,
            MinimumPrice = summaries.Min(x => x.MinPrice),
            MaximumPrice = summaries.Max(x => x.MaxPrice),
            AveragePrice = submissionCount == 0 ? 0 : weightedTotal / submissionCount,
            SubmissionCount = submissionCount,
            FromDate = summaries.Min(x => x.PriceDate),
            ToDate = summaries.Max(x => x.PriceDate)
        });
    }

    private Task<IReadOnlyList<DailyPriceSummary>> GetSummariesAsync(
        PriceSearchRequest request,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        return _priceSummaryRepository.GetAsync(
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
}
