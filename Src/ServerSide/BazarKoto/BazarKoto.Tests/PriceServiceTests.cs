using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Services;
using BazarKoto.Contracts.Admin;
using BazarKoto.Contracts.Common;
using BazarKoto.Contracts.Prices;
using BazarKoto.Contracts.UserTracking;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BazarKoto.Tests;

public class PriceServiceTests
{
    private readonly Mock<IPriceRepository> _priceRepository = new();
    private readonly Mock<IMarketRepository> _marketRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IPriceSummaryService> _priceSummaryService = new();
    private readonly Mock<IUserTrackingService> _userTrackingService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task SubmitPriceAsync_WithExistingMarketAndProduct_SavesAllSubmittedFields()
    {
        var marketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var divisionId = Guid.NewGuid();
        var districtId = Guid.NewGuid();
        var upazilaId = Guid.NewGuid();
        var unionOrWardId = Guid.NewGuid();
        var userTrackingDetailsId = Guid.NewGuid();
        var trackingGuid = Guid.NewGuid();
        PriceSubmission? savedPrice = null;

        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market
            {
                Id = marketId,
                MarketName = "Test Market",
                DivisionId = divisionId,
                DistrictId = districtId,
                UpazilaId = upazilaId,
                UnionOrWardId = unionOrWardId
            });
        _productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = productId, NameEn = "Potato", NameBn = "আলু" });
        _priceRepository.Setup(x => x.GetAsync(null, null, null, null, marketId, null, productId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _priceRepository.Setup(x => x.AddAsync(It.IsAny<PriceSubmission>(), It.IsAny<CancellationToken>()))
            .Callback<PriceSubmission, CancellationToken>((price, _) => savedPrice = price)
            .Returns(Task.CompletedTask);
        _userTrackingService.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserTrackingInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserTrackingResult
            {
                UserTrackingDetailsId = userTrackingDetailsId,
                TrackingGuid = trackingGuid
            });

        var service = CreateService();

        var response = await service.SubmitPriceAsync(new SubmitPriceRequest
        {
            MarketId = marketId,
            ProductId = productId,
            Unit = " kg ",
            PricePerUnit = 55m,
            QuantityChecked = 2m,
            PriceDate = new DateOnly(2026, 5, 23),
            PriceTime = new TimeOnly(9, 30),
            SellerType = "Retail",
            PriceSource = "ObservedInMarket",
            QualityGrade = "Premium",
            Notes = "  Clean, medium size  ",
        });

        response.Success.Should().BeTrue();
        savedPrice.Should().NotBeNull();
        savedPrice!.MarketId.Should().Be(marketId);
        savedPrice.ProductId.Should().Be(productId);
        savedPrice.DivisionId.Should().Be(divisionId);
        savedPrice.DistrictId.Should().Be(districtId);
        savedPrice.UpazilaId.Should().Be(upazilaId);
        savedPrice.UnionOrWardId.Should().Be(unionOrWardId);
        savedPrice.UserTrackingDetailsId.Should().Be(userTrackingDetailsId);
        savedPrice.TrackingGuid.Should().Be(trackingGuid);
        savedPrice.Unit.Should().Be("kg");
        savedPrice.PricePerUnit.Should().Be(55m);
        savedPrice.QuantityChecked.Should().Be(2m);
        savedPrice.SellerType.Should().Be(SellerType.Retail);
        savedPrice.PriceSource.Should().Be(PriceSource.ObservedInMarket);
        savedPrice.QualityGrade.Should().Be(QualityGrade.Premium);
        savedPrice.Notes.Should().Be("Clean, medium size");
        savedPrice.Status.Should().Be(SubmissionStatus.Approved);
        response.Data!.TrackingGuid.Should().Be(trackingGuid);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitPriceAsync_WithTrackingFields_PassesMarketLocationToUserTrackingService()
    {
        var marketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var divisionId = Guid.NewGuid();
        var districtId = Guid.NewGuid();
        var upazilaId = Guid.NewGuid();
        var unionOrWardId = Guid.NewGuid();
        var requestTrackingGuid = Guid.NewGuid();
        UserTrackingInput? trackingInput = null;

        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market
            {
                Id = marketId,
                DivisionId = divisionId,
                DistrictId = districtId,
                UpazilaId = upazilaId,
                UnionOrWardId = unionOrWardId
            });
        _productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = productId, NameEn = "Potato", NameBn = "আলু" });
        _priceRepository.Setup(x => x.GetAsync(null, null, null, null, marketId, null, productId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _userTrackingService.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserTrackingInput>(), It.IsAny<CancellationToken>()))
            .Callback<UserTrackingInput, CancellationToken>((input, _) => trackingInput = input)
            .ReturnsAsync(new UserTrackingResult
            {
                UserTrackingDetailsId = Guid.NewGuid(),
                TrackingGuid = requestTrackingGuid
            });

        var service = CreateService();

        await service.SubmitPriceAsync(new SubmitPriceRequest
        {
            MarketId = marketId,
            ProductId = productId,
            Unit = "kg",
            PricePerUnit = 55m,
            PriceDate = new DateOnly(2026, 5, 23),
            SellerType = "Retail",
            PriceSource = "ObservedInMarket",
            QualityGrade = "Standard",
            TrackingGuid = requestTrackingGuid,
            GpsLatitude = 23.810331m,
            GpsLongitude = 90.412521m,
            GpsAccuracyMeters = 15.5m,
            GpsPermissionStatus = "granted",
            IpBasedCountry = "Bangladesh",
            IpBasedRegion = "Dhaka",
            IpBasedCity = "Dhaka",
            IpBasedLatitude = 23.800000m,
            IpBasedLongitude = 90.400000m,
            IpLocationProvider = "Test Provider",
            IpLocationAccuracy = "Approximate",
            LocationSource = "manual"
        });

        trackingInput.Should().NotBeNull();
        trackingInput!.TrackingGuid.Should().Be(requestTrackingGuid);
        trackingInput.GpsLatitude.Should().Be(23.810331m);
        trackingInput.GpsLongitude.Should().Be(90.412521m);
        trackingInput.GpsAccuracyMeters.Should().Be(15.5m);
        trackingInput.GpsPermissionStatus.Should().Be("granted");
        trackingInput.IpBasedCountry.Should().Be("Bangladesh");
        trackingInput.IpBasedRegion.Should().Be("Dhaka");
        trackingInput.IpBasedCity.Should().Be("Dhaka");
        trackingInput.IpBasedLatitude.Should().Be(23.800000m);
        trackingInput.IpBasedLongitude.Should().Be(90.400000m);
        trackingInput.IpLocationProvider.Should().Be("Test Provider");
        trackingInput.IpLocationAccuracy.Should().Be("Approximate");
        trackingInput.LastKnownDivisionId.Should().Be(divisionId);
        trackingInput.LastKnownDistrictId.Should().Be(districtId);
        trackingInput.LastKnownUpazilaId.Should().Be(upazilaId);
        trackingInput.LastKnownUnionOrWardId.Should().Be(unionOrWardId);
        trackingInput.LocationSource.Should().Be("gps");
    }

    [Fact]
    public async Task SubmitPriceAsync_WithoutTrackingFields_StillSubmitsAndUsesMarketLocationSource()
    {
        var marketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        UserTrackingInput? trackingInput = null;

        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market
            {
                Id = marketId,
                DivisionId = Guid.NewGuid(),
                DistrictId = Guid.NewGuid(),
                UpazilaId = Guid.NewGuid(),
                UnionOrWardId = null
            });
        _productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = productId, NameEn = "Potato", NameBn = "আলু" });
        _priceRepository.Setup(x => x.GetAsync(null, null, null, null, marketId, null, productId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _priceRepository.Setup(x => x.AddAsync(It.IsAny<PriceSubmission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userTrackingService.Setup(x => x.CreateOrUpdateAsync(It.IsAny<UserTrackingInput>(), It.IsAny<CancellationToken>()))
            .Callback<UserTrackingInput, CancellationToken>((input, _) => trackingInput = input)
            .ReturnsAsync(new UserTrackingResult
            {
                UserTrackingDetailsId = Guid.NewGuid(),
                TrackingGuid = Guid.NewGuid()
            });

        var service = CreateService();

        var response = await service.SubmitPriceAsync(new SubmitPriceRequest
        {
            MarketId = marketId,
            ProductId = productId,
            Unit = "kg",
            PricePerUnit = 55m,
            PriceDate = new DateOnly(2026, 5, 23),
            SellerType = "Retail",
            PriceSource = "ObservedInMarket",
            QualityGrade = "Standard"
        });

        response.Success.Should().BeTrue();
        trackingInput.Should().NotBeNull();
        trackingInput!.TrackingGuid.Should().BeNull();
        trackingInput.LocationSource.Should().Be("market");
    }

    [Fact]
    public async Task UpdatePriceAsync_WithMatchingSelection_UpdatesOnlyPricePerUnit()
    {
        var marketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var priceId = Guid.NewGuid();
        var price = new PriceSubmission
        {
            Id = priceId,
            MarketId = marketId,
            ProductId = productId,
            Unit = "kg",
            PricePerUnit = 55m,
            QuantityChecked = 2m,
            PriceDate = new DateOnly(2026, 5, 23),
            SellerType = SellerType.Retail,
            PriceSource = PriceSource.ObservedInMarket,
            QualityGrade = QualityGrade.Standard,
            Notes = "Original notes",
            Status = SubmissionStatus.Pending,
        };

        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market { Id = marketId, MarketName = "Test Market" });
        _productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = productId, NameEn = "Potato", NameBn = "আলু" });
        _priceRepository.Setup(x => x.GetByIdAsync(priceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(price);

        var service = CreateService();

        var response = await service.UpdatePriceAsync(priceId, new UpdatePriceRequest
        {
            MarketId = marketId,
            ProductId = productId,
            Unit = "piece",
            PricePerUnit = 60m,
            QuantityChecked = 10m,
            PriceDate = new DateOnly(2026, 5, 24),
            SellerType = "Wholesale",
            PriceSource = "SellerProvided",
            QualityGrade = "Premium",
            Notes = "Changed notes",
        });

        response.Success.Should().BeTrue();
        price.PricePerUnit.Should().Be(60m);
        price.Unit.Should().Be("kg");
        price.QuantityChecked.Should().Be(2m);
        price.SellerType.Should().Be(SellerType.Retail);
        price.PriceSource.Should().Be(PriceSource.ObservedInMarket);
        price.QualityGrade.Should().Be(QualityGrade.Standard);
        price.Notes.Should().Be("Original notes");
        price.Status.Should().Be(SubmissionStatus.Approved);
        _priceRepository.Verify(x => x.Update(price), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPendingPricesAsync_UsesPagedRepositoryResult()
    {
        var prices = new[]
        {
            CreatePendingPrice("Onion"),
            CreatePendingPrice("Potato")
        };

        _priceRepository.Setup(x => x.GetPendingPageAsync(2, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((prices, 5));

        var service = CreateService();

        var response = await service.GetPendingPricesAsync(new PaginationRequest
        {
            PageNumber = 2,
            PageSize = 2
        });

        response.Data.Should().HaveCount(2);
        response.PageNumber.Should().Be(2);
        response.PageSize.Should().Be(2);
        response.TotalCount.Should().Be(5);
        response.TotalPages.Should().Be(3);
        response.Data.Select(x => x.ProductNameEn).Should().Equal("Onion", "Potato");
        _priceRepository.Verify(x => x.GetPendingPageAsync(2, 2, It.IsAny<CancellationToken>()), Times.Once);
        _priceRepository.Verify(x => x.GetPendingAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAdminPriceAsync_WithValidRequest_UpdatesEditableFields()
    {
        var priceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var marketId = Guid.NewGuid();
        var divisionId = Guid.NewGuid();
        var districtId = Guid.NewGuid();
        var upazilaId = Guid.NewGuid();
        var unionOrWardId = Guid.NewGuid();
        var price = new PriceSubmission
        {
            Id = priceId,
            ProductId = Guid.NewGuid(),
            MarketId = Guid.NewGuid(),
            Unit = "kg",
            PricePerUnit = 55m,
            PriceDate = new DateOnly(2026, 5, 23),
            PriceTime = new TimeOnly(9, 30),
            PriceSource = PriceSource.UserReported,
            Status = SubmissionStatus.Pending,
            Notes = "Old notes",
        };
        var product = new Product
        {
            Id = productId,
            NameEn = "Potato",
            NameBn = "আলু",
            LocalName = "Local potato",
        };
        var market = new Market
        {
            Id = marketId,
            MarketName = "Test Market",
            Area = "Main road",
            VillageOrMoholla = "North para",
            DivisionId = divisionId,
            DistrictId = districtId,
            UpazilaId = upazilaId,
            UnionOrWardId = unionOrWardId,
        };

        _priceRepository.Setup(x => x.GetByIdAsync(priceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(price);
        _productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(market);

        var service = CreateService();

        var response = await service.UpdateAdminPriceAsync(priceId, new AdminUpdatePriceRequest
        {
            ProductId = productId,
            MarketId = marketId,
            Price = 70m,
            Unit = " piece ",
            PriceDate = new DateOnly(2026, 5, 24),
            PriceTime = new TimeOnly(14, 15),
            Status = "Approved",
            Source = "ObservedInMarket",
            Notes = " Updated notes ",
        });

        response.Success.Should().BeTrue();
        price.ProductId.Should().Be(productId);
        price.MarketId.Should().Be(marketId);
        price.DivisionId.Should().Be(divisionId);
        price.DistrictId.Should().Be(districtId);
        price.UpazilaId.Should().Be(upazilaId);
        price.UnionOrWardId.Should().Be(unionOrWardId);
        price.PricePerUnit.Should().Be(70m);
        price.Unit.Should().Be("piece");
        price.PriceDate.Should().Be(new DateOnly(2026, 5, 24));
        price.PriceTime.Should().Be(new TimeOnly(14, 15));
        price.Status.Should().Be(SubmissionStatus.Approved);
        price.PriceSource.Should().Be(PriceSource.ObservedInMarket);
        price.Notes.Should().Be("Updated notes");
        price.UpdatedAt.Should().NotBeNull();
        response.Data!.ProductName.Should().Be("Potato");
        response.Data.Price.Should().Be(70m);
        response.Data.Source.Should().Be("ObservedInMarket");
        _priceRepository.Verify(x => x.Update(price), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAdminPriceAsync_WithMissingPrice_ReturnsFailureWithoutSaving()
    {
        var priceId = Guid.NewGuid();

        _priceRepository.Setup(x => x.GetByIdAsync(priceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PriceSubmission?)null);

        var service = CreateService();

        var response = await service.UpdateAdminPriceAsync(priceId, new AdminUpdatePriceRequest
        {
            Price = 70m,
        });

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Price submission was not found.");
        _priceRepository.Verify(x => x.Update(It.IsAny<PriceSubmission>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAdminPriceAsync_WithInvalidPrice_ReturnsValidationFailureWithoutSaving()
    {
        var service = CreateService();

        var response = await service.UpdateAdminPriceAsync(Guid.NewGuid(), new AdminUpdatePriceRequest
        {
            Price = 0m,
        });

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Validation failed.");
        response.Errors.Should().Contain("Price must be greater than zero.");
        _priceRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _priceRepository.Verify(x => x.Update(It.IsAny<PriceSubmission>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAdminPriceAsync_WithMissingMarket_ReturnsValidationFailureWithoutSaving()
    {
        var priceId = Guid.NewGuid();
        var marketId = Guid.NewGuid();
        var price = new PriceSubmission
        {
            Id = priceId,
            ProductId = Guid.NewGuid(),
            MarketId = Guid.NewGuid(),
            Unit = "kg",
            PricePerUnit = 55m,
            PriceDate = new DateOnly(2026, 5, 23),
        };

        _priceRepository.Setup(x => x.GetByIdAsync(priceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(price);
        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Market?)null);

        var service = CreateService();

        var response = await service.UpdateAdminPriceAsync(priceId, new AdminUpdatePriceRequest
        {
            MarketId = marketId,
            Price = 70m,
        });

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Validation failed.");
        response.Errors.Should().Contain("Selected market was not found.");
        _priceRepository.Verify(x => x.Update(It.IsAny<PriceSubmission>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitPriceAsync_WithMissingMarket_ReturnsValidationFailureWithoutSaving()
    {
        var productId = Guid.NewGuid();

        _productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = productId, NameEn = "Potato", NameBn = "আলু" });

        var service = CreateService();

        var response = await service.SubmitPriceAsync(new SubmitPriceRequest
        {
            MarketId = Guid.NewGuid(),
            ProductId = productId,
            Unit = "kg",
            PricePerUnit = 55m,
            PriceDate = new DateOnly(2026, 5, 23),
            SellerType = "Retail",
            PriceSource = "ObservedInMarket",
            QualityGrade = "Standard",
        });

        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Selected market was not found");
        _priceRepository.Verify(x => x.AddAsync(It.IsAny<PriceSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
        _userTrackingService.Verify(x => x.CreateOrUpdateAsync(It.IsAny<UserTrackingInput>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private PriceService CreateService()
    {
        return new PriceService(
            _priceRepository.Object,
            _marketRepository.Object,
            _productRepository.Object,
            _priceSummaryService.Object,
            _userTrackingService.Object,
            _unitOfWork.Object);
    }

    private static PriceSubmission CreatePendingPrice(string productName)
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var marketId = Guid.NewGuid();

        return new PriceSubmission
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Product = new Product
            {
                Id = productId,
                NameEn = productName,
                NameBn = productName,
                CategoryId = categoryId,
                Category = new ProductCategory
                {
                    Id = categoryId,
                    NameEn = "Vegetables",
                    NameBn = "Vegetables"
                }
            },
            MarketId = marketId,
            Market = new Market
            {
                Id = marketId,
                MarketName = "Test Market"
            },
            Unit = "kg",
            PricePerUnit = 50m,
            PriceDate = new DateOnly(2026, 6, 5),
            SellerType = SellerType.Retail,
            PriceSource = PriceSource.UserReported,
            QualityGrade = QualityGrade.Standard,
            Status = SubmissionStatus.Pending
        };
    }
}
