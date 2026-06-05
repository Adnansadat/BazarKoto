using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Services;
using BazarKoto.Contracts.Prices;
using BazarKoto.Contracts.UserTracking;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BazarKoto.Tests;

public class PublicProductPriceServiceTests
{
    private readonly Mock<IPriceRepository> _priceRepository = new();
    private readonly Mock<IMarketRepository> _marketRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IPriceSummaryService> _priceSummaryService = new();
    private readonly Mock<IUserTrackingService> _userTrackingService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task GetPublicProductPricesAsync_WithoutUnionOrWardOrMarket_ReturnsEmptyWithoutRepositoryQuery()
    {
        var service = CreateService();

        var response = await service.GetPublicProductPricesAsync(new PublicProductPriceSearchRequest());

        response.Success.Should().BeTrue();
        response.Data.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.Message.Should().Contain("Select a Union/Ward or Market");
        _priceRepository.Verify(x => x.GetPublicProductPricesPageAsync(
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateOnly?>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPublicProductPricesAsync_WithUnionOrWard_ReturnsScopedPrices()
    {
        var unionOrWardId = Guid.NewGuid();
        var otherUnionOrWardId = Guid.NewGuid();
        var scopedPrice = CreatePrice(unionOrWardId: unionOrWardId);
        _priceRepository.Setup(x => x.GetPublicProductPricesPageAsync(null, null, null, unionOrWardId, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([scopedPrice], 1));

        var service = CreateService();

        var response = await service.GetPublicProductPricesAsync(new PublicProductPriceSearchRequest { UnionOrWardId = unionOrWardId });

        response.Data.Should().ContainSingle();
        response.Data[0].UnionOrWardId.Should().Be(unionOrWardId);
        response.Data[0].UnionOrWardId.Should().NotBe(otherUnionOrWardId);
    }

    [Fact]
    public async Task GetPublicProductPricesAsync_WithMarketId_ReturnsOnlyThatMarket()
    {
        var marketId = Guid.NewGuid();
        var unionOrWardId = Guid.NewGuid();
        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market { Id = marketId, UnionOrWardId = unionOrWardId });
        _priceRepository.Setup(x => x.GetPublicProductPricesPageAsync(null, null, null, null, marketId, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([CreatePrice(marketId: marketId, unionOrWardId: unionOrWardId, marketName: "Bou Bazar")], 1));

        var service = CreateService();

        var response = await service.GetPublicProductPricesAsync(new PublicProductPriceSearchRequest { MarketId = marketId });

        response.Data.Should().ContainSingle();
        response.Data[0].MarketId.Should().Be(marketId);
        response.Data[0].MarketName.Should().Be("Bou Bazar");
    }

    [Fact]
    public async Task GetPublicProductPricesAsync_WithUnionOrWardAndProduct_ReturnsThatProductInUnion()
    {
        var unionOrWardId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _priceRepository.Setup(x => x.GetPublicProductPricesPageAsync(null, null, null, unionOrWardId, null, null, productId, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([CreatePrice(productId: productId, unionOrWardId: unionOrWardId)], 1));

        var service = CreateService();

        var response = await service.GetPublicProductPricesAsync(new PublicProductPriceSearchRequest
        {
            UnionOrWardId = unionOrWardId,
            ProductId = productId
        });

        response.Data.Should().ContainSingle();
        response.Data[0].ProductId.Should().Be(productId);
        response.Data[0].UnionOrWardId.Should().Be(unionOrWardId);
    }

    [Fact]
    public async Task GetPublicProductPricesAsync_WithMarketAndProduct_ReturnsThatProductInMarket()
    {
        var marketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market { Id = marketId, UnionOrWardId = Guid.NewGuid() });
        _priceRepository.Setup(x => x.GetPublicProductPricesPageAsync(null, null, null, null, marketId, null, productId, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([CreatePrice(productId: productId, marketId: marketId)], 1));

        var service = CreateService();

        var response = await service.GetPublicProductPricesAsync(new PublicProductPriceSearchRequest
        {
            MarketId = marketId,
            ProductId = productId
        });

        response.Data.Should().ContainSingle();
        response.Data[0].ProductId.Should().Be(productId);
        response.Data[0].MarketId.Should().Be(marketId);
    }

    [Fact]
    public async Task GetPublicProductPricesAsync_WithSameMarketNames_UsesMarketIdNotName()
    {
        var selectedMarketId = Guid.NewGuid();
        var otherMarketId = Guid.NewGuid();
        _marketRepository.Setup(x => x.GetByIdAsync(selectedMarketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market { Id = selectedMarketId, MarketName = "Bou Bazar", UnionOrWardId = Guid.NewGuid() });
        _priceRepository.Setup(x => x.GetPublicProductPricesPageAsync(null, null, null, null, selectedMarketId, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([CreatePrice(marketId: selectedMarketId, marketName: "Bou Bazar")], 1));

        var service = CreateService();

        var response = await service.GetPublicProductPricesAsync(new PublicProductPriceSearchRequest { MarketId = selectedMarketId });

        response.Data.Should().ContainSingle();
        response.Data[0].MarketId.Should().Be(selectedMarketId);
        response.Data[0].MarketId.Should().NotBe(otherMarketId);
        response.Data[0].MarketName.Should().Be("Bou Bazar");
    }

    [Fact]
    public async Task GetPublicProductPricesAsync_WithMarketUnionMismatch_ReturnsEmptySafely()
    {
        var marketId = Guid.NewGuid();
        var requestedUnionOrWardId = Guid.NewGuid();
        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market { Id = marketId, UnionOrWardId = Guid.NewGuid() });

        var service = CreateService();

        var response = await service.GetPublicProductPricesAsync(new PublicProductPriceSearchRequest
        {
            MarketId = marketId,
            UnionOrWardId = requestedUnionOrWardId
        });

        response.Data.Should().BeEmpty();
        response.Message.Should().Contain("does not match");
        _priceRepository.Verify(x => x.GetPublicProductPricesPageAsync(
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<DateOnly?>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void PublicProductPriceResponse_DoesNotExposeRawTrackingFields()
    {
        var propertyNames = typeof(PublicProductPriceResponse).GetProperties().Select(x => x.Name).ToList();

        propertyNames.Should().NotContain([
            "RawIpAddress",
            "RawUserAgent",
            "BrowserName",
            "BrowserVersion",
            "DeviceType",
            "OS",
            "GpsLatitude",
            "GpsLongitude",
            "GpsAccuracyMeters",
            "IpBasedCountry",
            "IpBasedRegion",
            "IpBasedCity",
            "UserTrackingDetailsId",
            "TrackingGuid"
        ]);
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

    private static PriceSubmission CreatePrice(
        Guid? productId = null,
        Guid? marketId = null,
        Guid? unionOrWardId = null,
        string marketName = "Test Market")
    {
        var categoryId = Guid.NewGuid();
        var finalProductId = productId ?? Guid.NewGuid();
        var finalMarketId = marketId ?? Guid.NewGuid();
        var finalUnionOrWardId = unionOrWardId ?? Guid.NewGuid();

        return new PriceSubmission
        {
            Id = Guid.NewGuid(),
            ProductId = finalProductId,
            Product = new Product
            {
                Id = finalProductId,
                NameEn = "Potato",
                NameBn = "আলু",
                CategoryId = categoryId,
                Category = new ProductCategory
                {
                    Id = categoryId,
                    NameEn = "Vegetables",
                    NameBn = "সবজি"
                }
            },
            MarketId = finalMarketId,
            Market = new Market
            {
                Id = finalMarketId,
                MarketName = marketName,
                UnionOrWardId = finalUnionOrWardId,
                Division = new Division { Id = Guid.NewGuid(), NameEn = "Dhaka", NameBn = "ঢাকা" },
                District = new District { Id = Guid.NewGuid(), NameEn = "Tangail", NameBn = "টাঙ্গাইল" },
                Upazila = new Upazila { Id = Guid.NewGuid(), NameEn = "Tangail Sadar", NameBn = "টাঙ্গাইল সদর" },
                UnionOrWard = new UnionOrWard { Id = finalUnionOrWardId, NameEn = "Ward 6", NameBn = "৬ নম্বর ওয়ার্ড" }
            },
            UnionOrWardId = finalUnionOrWardId,
            Unit = "kg",
            PricePerUnit = 55m,
            QuantityChecked = 1m,
            PriceDate = new DateOnly(2026, 5, 29),
            PriceTime = new TimeOnly(9, 30),
            SellerType = SellerType.Retail,
            PriceSource = PriceSource.UserReported,
            QualityGrade = QualityGrade.Standard,
            Status = SubmissionStatus.Approved
        };
    }
}
