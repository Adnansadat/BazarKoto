using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.Admin;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Prices;
using BazarKoto.Contracts.UserTracking;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;

namespace BazarKoto.Application.Services;

public class PriceService : IPriceService
{
    private readonly IPriceRepository _priceRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPriceSummaryService _priceSummaryService;
    private readonly IUserTrackingService _userTrackingService;
    private readonly IUnitOfWork _unitOfWork;

    public PriceService(
        IPriceRepository priceRepository,
        IMarketRepository marketRepository,
        IProductRepository productRepository,
        IPriceSummaryService priceSummaryService,
        IUserTrackingService userTrackingService,
        IUnitOfWork unitOfWork)
    {
        _priceRepository = priceRepository;
        _marketRepository = marketRepository;
        _productRepository = productRepository;
        _priceSummaryService = priceSummaryService;
        _userTrackingService = userTrackingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<PriceSubmissionResponse>> GetPricesAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        var prices = await _priceRepository.GetAsync(
            divisionId: request.DivisionId,
            districtId: request.DistrictId,
            upazilaId: request.UpazilaId,
            unionOrWardId: request.UnionOrWardId,
            marketId: request.MarketId,
            categoryId: request.CategoryId,
            productId: request.ProductId,
            date: request.Date,
            status: ParseSubmissionStatus(request.Status),
            cancellationToken: cancellationToken);

        return Page(prices.Select(ToResponse), request);
    }

    public async Task<AdminPriceListResponse> GetAdminPricesAsync(AdminPriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var status = ParseAdminSubmissionStatus(request.Status);

        if (!string.IsNullOrWhiteSpace(request.Status) && !status.HasValue)
        {
            return EmptyAdminPricePage(request, "Selected price status is not supported.");
        }

        var submittedFrom = request.DateFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var submittedTo = request.DateTo?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        if (submittedFrom.HasValue && submittedTo.HasValue && submittedFrom.Value > submittedTo.Value)
        {
            return EmptyAdminPricePage(request, "Date from must be before date to.");
        }

        var (items, totalCount) = await _priceRepository.GetAdminPricesAsync(
            search: normalizedSearch,
            status: status,
            submittedFrom: submittedFrom,
            submittedTo: submittedTo,
            productId: request.ProductId,
            marketId: request.MarketId,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        return ToAdminPriceListResponse(items, totalCount, request);
    }

    public async Task<ApiResponse<AdminPriceRecordResponse>> UpdateAdminPriceAsync(Guid id, AdminUpdatePriceRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAdminUpdatePriceRequest(request);

        if (validationErrors.Count > 0)
        {
            return ApiResponse<AdminPriceRecordResponse>.Fail("Validation failed.", validationErrors);
        }

        var priceSubmission = await _priceRepository.GetByIdAsync(id, cancellationToken);

        if (priceSubmission is null)
        {
            return ApiResponse<AdminPriceRecordResponse>.Fail("Price submission was not found.");
        }

        if (request.ProductId.HasValue)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId.Value, cancellationToken);

            if (product is null)
            {
                return ApiResponse<AdminPriceRecordResponse>.Fail("Validation failed.", ["Selected product was not found."]);
            }

            priceSubmission.ProductId = product.Id;
            priceSubmission.Product = product;
        }

        if (request.MarketId.HasValue)
        {
            var market = await _marketRepository.GetByIdAsync(request.MarketId.Value, cancellationToken);

            if (market is null)
            {
                return ApiResponse<AdminPriceRecordResponse>.Fail("Validation failed.", ["Selected market was not found."]);
            }

            priceSubmission.MarketId = market.Id;
            priceSubmission.Market = market;
            priceSubmission.DivisionId = market.DivisionId;
            priceSubmission.DistrictId = market.DistrictId;
            priceSubmission.UpazilaId = market.UpazilaId;
            priceSubmission.UnionOrWardId = market.UnionOrWardId;
        }

        if (request.Price.HasValue)
        {
            priceSubmission.PricePerUnit = request.Price.Value;
        }

        if (request.Unit is not null)
        {
            priceSubmission.Unit = request.Unit.Trim();
        }

        if (request.PriceDate.HasValue)
        {
            priceSubmission.PriceDate = request.PriceDate.Value;
        }

        if (request.PriceTime.HasValue)
        {
            priceSubmission.PriceTime = request.PriceTime.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            priceSubmission.Status = ParseEnum(request.Status.Trim(), priceSubmission.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            priceSubmission.PriceSource = ParseEnum(request.Source.Trim(), priceSubmission.PriceSource);
        }

        if (request.Notes is not null)
        {
            priceSubmission.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        }

        priceSubmission.UpdatedAt = DateTime.UtcNow;

        _priceRepository.Update(priceSubmission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AdminPriceRecordResponse>.Ok(ToAdminPriceRecordResponse(priceSubmission), "Price record updated successfully.");
    }

    public async Task<PagedResponse<PublicProductPriceResponse>> GetPublicProductPricesAsync(PublicProductPriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.MarketId.HasValue && !request.UnionOrWardId.HasValue)
        {
            return EmptyPage<PublicProductPriceResponse>(request, "Select a Union/Ward or Market to view local product prices.");
        }

        if (request.MarketId.HasValue)
        {
            var market = await _marketRepository.GetByIdAsync(request.MarketId.Value, cancellationToken);

            if (market is null)
            {
                return EmptyPage<PublicProductPriceResponse>(request, "Selected market was not found.");
            }

            if (request.UnionOrWardId.HasValue && market.UnionOrWardId != request.UnionOrWardId.Value)
            {
                return EmptyPage<PublicProductPriceResponse>(request, "Selected market does not match the selected Union/Ward.");
            }
        }

        var prices = await _priceRepository.GetPublicProductPricesAsync(
            divisionId: request.DivisionId,
            districtId: request.DistrictId,
            upazilaId: request.UpazilaId,
            unionOrWardId: request.MarketId.HasValue ? null : request.UnionOrWardId,
            marketId: request.MarketId,
            categoryId: request.CategoryId,
            productId: request.ProductId,
            date: request.Date,
            search: request.Search,
            cancellationToken: cancellationToken);

        return Page(prices.Select(ToPublicResponse), request);
    }

    public async Task<ApiResponse<PriceSubmissionResponse>> GetLatestPriceAsync(PriceSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.MarketId.HasValue || !request.ProductId.HasValue)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("Select an existing market and product to load price data.");
        }

        var prices = await _priceRepository.GetAsync(
            marketId: request.MarketId,
            productId: request.ProductId,
            cancellationToken: cancellationToken);

        var latestPrice = prices.FirstOrDefault();

        if (latestPrice is null)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("No existing price found for this market and product.");
        }

        return ApiResponse<PriceSubmissionResponse>.Ok(ToResponse(latestPrice), "Latest price loaded successfully.");
    }

    public async Task<ApiResponse<PriceSubmissionResponse>> SubmitPriceAsync(SubmitPriceRequest request, CancellationToken cancellationToken = default)
    {
        var market = await _marketRepository.GetByIdAsync(request.MarketId, cancellationToken);

        if (market is null)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("Selected market was not found. Please go back to Markets and select an existing market.");
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("Selected product was not found. Please go back to Products and select an existing product.");
        }

        var existingPrices = await _priceRepository.GetAsync(
            marketId: request.MarketId,
            productId: request.ProductId,
            cancellationToken: cancellationToken);

        if (existingPrices.Any())
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("A price already exists for this market and product. Change the price per unit to update the existing record.");
        }

        var tracking = await _userTrackingService.CreateOrUpdateAsync(BuildUserTrackingInput(request, market), cancellationToken);

        var priceSubmission = new PriceSubmission
        {
            MarketId = request.MarketId,
            Market = market,
            ProductId = request.ProductId,
            Product = product,
            DivisionId = market.DivisionId,
            DistrictId = market.DistrictId,
            UpazilaId = market.UpazilaId,
            UnionOrWardId = market.UnionOrWardId,
            UserTrackingDetailsId = tracking.UserTrackingDetailsId,
            TrackingGuid = tracking.TrackingGuid,
            Unit = request.Unit.Trim(),
            PricePerUnit = request.PricePerUnit,
            QuantityChecked = request.QuantityChecked,
            PriceDate = request.PriceDate,
            PriceTime = request.PriceTime,
            SellerType = ParseEnum(request.SellerType, SellerType.Retail),
            PriceSource = ParseEnum(request.PriceSource, PriceSource.UserReported),
            QualityGrade = ParseEnum(request.QualityGrade, QualityGrade.Standard),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = SubmissionStatus.Approved
        };

        await _priceRepository.AddAsync(priceSubmission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PriceSubmissionResponse>.Ok(ToResponse(priceSubmission), "Price submitted successfully.");
    }

    public async Task<ApiResponse<PriceSubmissionResponse>> UpdatePriceAsync(Guid id, UpdatePriceRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PricePerUnit <= 0)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("Price per unit must be greater than zero.");
        }

        var selectionValidation = await ValidateMarketProductSelectionAsync(request.MarketId, request.ProductId, cancellationToken);

        if (!selectionValidation.Success)
        {
            return selectionValidation;
        }

        var priceSubmission = await _priceRepository.GetByIdAsync(id, cancellationToken);

        if (priceSubmission is null)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("Price submission was not found.");
        }

        if (priceSubmission.MarketId != request.MarketId || priceSubmission.ProductId != request.ProductId)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("The selected price does not match this market and product.");
        }

        priceSubmission.PricePerUnit = request.PricePerUnit;
        priceSubmission.Status = SubmissionStatus.Approved;

        _priceRepository.Update(priceSubmission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<PriceSubmissionResponse>.Ok(ToResponse(priceSubmission), "Price updated successfully.");
    }

    private async Task<ApiResponse<PriceSubmissionResponse>> ValidateMarketProductSelectionAsync(Guid marketId, Guid productId, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(marketId, cancellationToken);

        if (market is null)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("Selected market was not found. Please go back to Markets and select an existing market.");
        }

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
        {
            return ApiResponse<PriceSubmissionResponse>.Fail("Selected product was not found. Please go back to Products and select an existing product.");
        }

        return ApiResponse<PriceSubmissionResponse>.Ok(new PriceSubmissionResponse());
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
            TrackingGuid = price.TrackingGuid
        };
    }

    private static PublicProductPriceResponse ToPublicResponse(PriceSubmission price)
    {
        return new PublicProductPriceResponse
        {
            Id = price.Id,
            ProductId = price.ProductId,
            ProductNameEn = price.Product?.NameEn ?? string.Empty,
            ProductNameBn = price.Product?.NameBn ?? string.Empty,
            CategoryId = price.Product?.CategoryId ?? Guid.Empty,
            CategoryNameEn = price.Product?.Category?.NameEn ?? string.Empty,
            CategoryNameBn = price.Product?.Category?.NameBn ?? string.Empty,
            MarketId = price.MarketId,
            MarketName = price.Market?.MarketName ?? string.Empty,
            DivisionId = price.DivisionId ?? price.Market?.DivisionId,
            DivisionNameEn = price.Market?.Division?.NameEn ?? string.Empty,
            DivisionNameBn = price.Market?.Division?.NameBn ?? string.Empty,
            DistrictId = price.DistrictId ?? price.Market?.DistrictId,
            DistrictNameEn = price.Market?.District?.NameEn ?? string.Empty,
            DistrictNameBn = price.Market?.District?.NameBn ?? string.Empty,
            UpazilaId = price.UpazilaId ?? price.Market?.UpazilaId,
            UpazilaNameEn = price.Market?.Upazila?.NameEn ?? string.Empty,
            UpazilaNameBn = price.Market?.Upazila?.NameBn ?? string.Empty,
            UnionOrWardId = price.UnionOrWardId ?? price.Market?.UnionOrWardId,
            UnionOrWardNameEn = price.Market?.UnionOrWard?.NameEn,
            UnionOrWardNameBn = price.Market?.UnionOrWard?.NameBn,
            Unit = price.Unit,
            PricePerUnit = price.PricePerUnit,
            QuantityChecked = price.QuantityChecked,
            PriceDate = price.PriceDate,
            PriceTime = price.PriceTime,
            SellerType = price.SellerType.ToString(),
            PriceSource = price.PriceSource.ToString(),
            QualityGrade = price.QualityGrade.ToString(),
            Notes = price.Notes,
            Status = price.Status.ToString()
        };
    }

    private static AdminPriceListResponse ToAdminPriceListResponse(
        IReadOnlyList<PriceSubmission> prices,
        int totalCount,
        AdminPriceSearchRequest request)
    {
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new AdminPriceListResponse
        {
            Message = "Success",
            Data = prices.Select(ToAdminPriceRecordResponse).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = request.PageNumber > 1 && totalPages > 0,
            HasNextPage = request.PageNumber < totalPages
        };
    }

    private static AdminPriceListResponse EmptyAdminPricePage(AdminPriceSearchRequest request, string message)
    {
        return new AdminPriceListResponse
        {
            Message = message,
            Data = [],
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = 0,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };
    }

    private static AdminPriceRecordResponse ToAdminPriceRecordResponse(PriceSubmission price)
    {
        return new AdminPriceRecordResponse
        {
            Id = price.Id,
            ProductId = price.ProductId,
            ProductName = price.Product?.NameEn ?? string.Empty,
            ProductLocalName = price.Product?.LocalName,
            AlternateName = price.Product?.NameBn,
            MarketId = price.MarketId,
            MarketName = price.Market?.MarketName ?? string.Empty,
            Location = BuildLocationLabel(price),
            Price = price.PricePerUnit,
            Unit = price.Unit,
            SubmittedAt = price.CreatedAt,
            Status = price.Status.ToString(),
            Source = price.PriceSource.ToString(),
            UpdatedAt = price.UpdatedAt
        };
    }

    private static string BuildLocationLabel(PriceSubmission price)
    {
        var parts = new[]
        {
            price.Market?.VillageOrMoholla,
            price.Market?.Area,
            price.Market?.UnionOrWard?.NameEn,
            price.Market?.Upazila?.NameEn,
            price.Market?.District?.NameEn,
            price.Market?.Division?.NameEn
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    private static List<string> ValidateAdminUpdatePriceRequest(AdminUpdatePriceRequest request)
    {
        var errors = new List<string>();

        if (request.ProductId == Guid.Empty)
        {
            errors.Add("Product is required.");
        }

        if (request.MarketId == Guid.Empty)
        {
            errors.Add("Market is required.");
        }

        if (request.Price.HasValue && request.Price.Value <= 0)
        {
            errors.Add("Price must be greater than zero.");
        }

        if (request.Unit is not null && string.IsNullOrWhiteSpace(request.Unit))
        {
            errors.Add("Unit is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && !Enum.TryParse<SubmissionStatus>(request.Status.Trim(), true, out _))
        {
            errors.Add("Status is not supported.");
        }

        if (!string.IsNullOrWhiteSpace(request.Source) && !Enum.TryParse<PriceSource>(request.Source.Trim(), true, out _))
        {
            errors.Add("Source is not supported.");
        }

        return errors;
    }

    private static UserTrackingInput BuildUserTrackingInput(SubmitPriceRequest request, Market market)
    {
        return new UserTrackingInput
        {
            TrackingGuid = request.TrackingGuid,
            GpsLatitude = request.GpsLatitude,
            GpsLongitude = request.GpsLongitude,
            GpsAccuracyMeters = request.GpsAccuracyMeters,
            GpsPermissionStatus = request.GpsPermissionStatus,
            IpBasedCountry = request.IpBasedCountry,
            IpBasedRegion = request.IpBasedRegion,
            IpBasedCity = request.IpBasedCity,
            IpBasedLatitude = request.IpBasedLatitude,
            IpBasedLongitude = request.IpBasedLongitude,
            IpLocationProvider = request.IpLocationProvider,
            IpLocationAccuracy = request.IpLocationAccuracy,
            LastKnownDivisionId = market.DivisionId,
            LastKnownDistrictId = market.DistrictId,
            LastKnownUpazilaId = market.UpazilaId,
            LastKnownUnionOrWardId = market.UnionOrWardId,
            LocationSource = ResolveLocationSource(request)
        };
    }

    private static string ResolveLocationSource(SubmitPriceRequest request)
    {
        if (request.GpsLatitude.HasValue
            && request.GpsLongitude.HasValue
            && string.Equals(request.GpsPermissionStatus, "granted", StringComparison.OrdinalIgnoreCase))
        {
            return "gps";
        }

        return string.IsNullOrWhiteSpace(request.LocationSource) ? "market" : request.LocationSource.Trim();
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

    private static PagedResponse<T> EmptyPage<T>(PaginationRequest request, string message)
    {
        return new PagedResponse<T>
        {
            Success = true,
            Message = message,
            Data = [],
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = 0,
            TotalPages = 0
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private static SubmissionStatus? ParseSubmissionStatus(string? value)
    {
        return Enum.TryParse<SubmissionStatus>(value, true, out var parsed) ? parsed : null;
    }

    private static SubmissionStatus? ParseAdminSubmissionStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<SubmissionStatus>(value.Trim(), true, out var parsed) ? parsed : null;
    }
}
