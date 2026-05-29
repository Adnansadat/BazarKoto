using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Services;
using BazarKoto.Contracts.Markets;
using BazarKoto.Contracts.Products;
using BazarKoto.Domain.Entities;
using BazarKoto.Domain.Enums;
using BazarKoto.Infrastructure.Persistence;
using BazarKoto.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BazarKoto.Tests;

public class SearchOptionServiceTests
{
    [Fact]
    public async Task GetMarketOptionsAsync_ReturnsMarketIdAndDisplayLabel()
    {
        var repository = new Mock<IMarketRepository>();
        var market = CreateMarket("Bou Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 6");
        repository.Setup(x => x.GetOptionsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([market]);
        repository.Setup(x => x.CountOptionsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new MarketService(repository.Object, Mock.Of<IUnitOfWork>());

        var response = await service.GetMarketOptionsAsync(new MarketSearchRequest { Search = "Bou" });

        response.Data.Should().ContainSingle();
        response.Data[0].Id.Should().Be(market.Id);
        response.Data[0].MarketId.Should().Be(market.Id);
        response.Data[0].MarketName.Should().Be("Bou Bazar");
        response.Data[0].DisplayLabel.Should().Be("Bou Bazar — Ward 6, Tangail Sadar, Tangail, Dhaka Division");
    }

    [Fact]
    public async Task GetMarketOptionsAsync_WithDuplicateNames_ReturnsDistinctIdsAndLabels()
    {
        var repository = new Mock<IMarketRepository>();
        var tangailMarket = CreateMarket("Bou Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 6");
        var dhakaMarket = CreateMarket("Bou Bazar", "Dhaka", "Dhaka", "Mirpur", "Ward 2");
        repository.Setup(x => x.GetOptionsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([tangailMarket, dhakaMarket]);
        repository.Setup(x => x.CountOptionsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var service = new MarketService(repository.Object, Mock.Of<IUnitOfWork>());

        var response = await service.GetMarketOptionsAsync(new MarketSearchRequest { Search = "Bou" });

        response.Data.Should().HaveCount(2);
        response.Data.Select(x => x.MarketId).Should().OnlyHaveUniqueItems();
        response.Data.Select(x => x.DisplayLabel).Should().OnlyHaveUniqueItems();
        response.Data.Select(x => x.DisplayLabel).Should().Contain(label => label.Contains("Tangail"));
        response.Data.Select(x => x.DisplayLabel).Should().Contain(label => label.Contains("Mirpur"));
    }

    [Fact]
    public async Task GetMarketOptionsAsync_RespectsUnionOrWardFilter()
    {
        var repository = new Mock<IMarketRepository>();
        var unionOrWardId = Guid.NewGuid();
        repository.Setup(x => x.GetOptionsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateMarket("Park Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 6", unionOrWardId)]);
        repository.Setup(x => x.CountOptionsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new MarketService(repository.Object, Mock.Of<IUnitOfWork>());

        var response = await service.GetMarketOptionsAsync(new MarketSearchRequest { UnionOrWardId = unionOrWardId });

        response.Data.Should().ContainSingle();
        response.Data[0].UnionOrWardId.Should().Be(unionOrWardId);
        repository.Verify(x => x.GetOptionsAsync(null, null, null, unionOrWardId, null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMarketOptionsAsync_AllowsGlobalSearchWithoutLocation()
    {
        var repository = new Mock<IMarketRepository>();
        repository.Setup(x => x.GetOptionsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateMarket("Bou Bazar", "Dhaka", "Tangail", "Tangail Sadar", null)]);
        repository.Setup(x => x.CountOptionsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new MarketService(repository.Object, Mock.Of<IUnitOfWork>());

        var response = await service.GetMarketOptionsAsync(new MarketSearchRequest { Search = "Bou" });

        response.Data.Should().ContainSingle();
        repository.Verify(x => x.GetOptionsAsync(null, null, null, null, "Bou", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductOptionsAsync_ReturnsProductIdAndDisplayLabel()
    {
        var repository = new Mock<IProductRepository>();
        var product = CreateProduct("Potato", "Vegetables", true, RecordStatus.Approved);
        repository.Setup(x => x.GetOptionsAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        repository.Setup(x => x.CountOptionsAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new ProductService(repository.Object, Mock.Of<IUnitOfWork>());

        var response = await service.GetProductOptionsAsync(new ProductSearchRequest { Search = "pot" });

        response.Data.Should().ContainSingle();
        response.Data[0].Id.Should().Be(product.Id);
        response.Data[0].ProductId.Should().Be(product.Id);
        response.Data[0].ProductNameEn.Should().Be("Potato");
        response.Data[0].DisplayLabel.Should().Be("Potato — Vegetables");
    }

    [Fact]
    public async Task GetProductOptionsAsync_UsesRepositoryPublicOptionFilter()
    {
        var repository = new Mock<IProductRepository>();
        repository.Setup(x => x.GetOptionsAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateProduct("Potato", "Vegetables", true, RecordStatus.Approved)]);
        repository.Setup(x => x.CountOptionsAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new ProductService(repository.Object, Mock.Of<IUnitOfWork>());

        var response = await service.GetProductOptionsAsync(new ProductSearchRequest());

        response.Data.Should().ContainSingle();
        response.Data[0].ProductNameEn.Should().Be("Potato");
        repository.Verify(x => x.GetOptionsAsync(null, null, null, null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductOptionsAsync_PassesScopeFiltersToRepository()
    {
        var repository = new Mock<IProductRepository>();
        var unionOrWardId = Guid.NewGuid();
        var marketId = Guid.NewGuid();
        repository.Setup(x => x.GetOptionsAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateProduct("Potato", "Vegetables", true, RecordStatus.Approved)]);
        repository.Setup(x => x.CountOptionsAsync(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new ProductService(repository.Object, Mock.Of<IUnitOfWork>());

        var response = await service.GetProductOptionsAsync(new ProductSearchRequest { UnionOrWardId = unionOrWardId, MarketId = marketId });

        response.Data.Should().ContainSingle();
        repository.Verify(x => x.GetOptionsAsync(null, null, unionOrWardId, marketId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarketRepository_GetOptionsAsync_ReturnsApprovedAndPendingMarketsOnly()
    {
        await using var dbContext = CreateDbContext();
        var approved = CreateMarket("Approved Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 1", status: RecordStatus.Approved);
        var pending = CreateMarket("Pending Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 1", status: RecordStatus.Pending);
        var rejected = CreateMarket("Rejected Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 1", status: RecordStatus.Rejected);
        var inactive = CreateMarket("Inactive Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 1", status: RecordStatus.Inactive);
        await dbContext.Markets.AddRangeAsync(approved, pending, rejected, inactive);
        await dbContext.SaveChangesAsync();
        var repository = new MarketRepository(dbContext);

        var markets = await repository.GetOptionsAsync();

        markets.Select(x => x.Id).Should().BeEquivalentTo([approved.Id, pending.Id]);
        markets.Select(x => x.Id).Should().NotContain([rejected.Id, inactive.Id]);
    }

    [Fact]
    public async Task MarketRepository_GetOptionsAsync_RespectsUnionOrWardFilter()
    {
        await using var dbContext = CreateDbContext();
        var selectedUnionId = Guid.NewGuid();
        var otherUnionId = Guid.NewGuid();
        var selectedMarket = CreateMarket("Selected Union Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 1", selectedUnionId, RecordStatus.Pending);
        var otherMarket = CreateMarket("Other Union Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 2", otherUnionId, RecordStatus.Pending);
        await dbContext.Markets.AddRangeAsync(selectedMarket, otherMarket);
        await dbContext.SaveChangesAsync();
        var repository = new MarketRepository(dbContext);

        var markets = await repository.GetOptionsAsync(unionOrWardId: selectedUnionId);

        markets.Should().ContainSingle();
        markets[0].Id.Should().Be(selectedMarket.Id);
    }

    [Fact]
    public async Task ProductRepository_GetOptionsAsync_ReturnsActiveApprovedAndPendingProductsOnly()
    {
        await using var dbContext = CreateDbContext();
        var approved = CreateProduct("Approved Potato", "Vegetables", true, RecordStatus.Approved);
        var pending = CreateProduct("Pending Potato", "Vegetables", true, RecordStatus.Pending);
        var rejected = CreateProduct("Rejected Potato", "Vegetables", true, RecordStatus.Rejected);
        var inactiveStatus = CreateProduct("Inactive Status Potato", "Vegetables", true, RecordStatus.Inactive);
        var inactiveFlag = CreateProduct("Inactive Flag Potato", "Vegetables", false, RecordStatus.Approved);
        await dbContext.Products.AddRangeAsync(approved, pending, rejected, inactiveStatus, inactiveFlag);
        await dbContext.SaveChangesAsync();
        var repository = new ProductRepository(dbContext);

        var products = await repository.GetOptionsAsync();

        products.Select(x => x.Id).Should().BeEquivalentTo([approved.Id, pending.Id]);
        products.Select(x => x.Id).Should().NotContain([rejected.Id, inactiveStatus.Id, inactiveFlag.Id]);
    }

    [Fact]
    public async Task ProductRepository_GetOptionsAsync_WithUnionOrWard_ReturnsProductsWithApprovedPricesInScope()
    {
        await using var dbContext = CreateDbContext();
        var selectedUnionId = Guid.NewGuid();
        var otherUnionId = Guid.NewGuid();
        var selectedMarket = CreateMarket("Selected Union Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 1", selectedUnionId);
        var otherMarket = CreateMarket("Other Union Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 2", otherUnionId);
        var approvedProduct = CreateProduct("Approved Potato", "Vegetables", true, RecordStatus.Approved);
        var pendingProduct = CreateProduct("Pending Onion", "Vegetables", true, RecordStatus.Pending);
        var rejectedProduct = CreateProduct("Rejected Rice", "Staples", true, RecordStatus.Rejected);
        var otherUnionProduct = CreateProduct("Other Union Fish", "Protein", true, RecordStatus.Approved);
        await dbContext.Markets.AddRangeAsync(selectedMarket, otherMarket);
        await dbContext.Products.AddRangeAsync(approvedProduct, pendingProduct, rejectedProduct, otherUnionProduct);
        await dbContext.PriceSubmissions.AddRangeAsync(
            CreatePrice(selectedMarket, approvedProduct, SubmissionStatus.Approved),
            CreatePrice(selectedMarket, pendingProduct, SubmissionStatus.Approved),
            CreatePrice(selectedMarket, rejectedProduct, SubmissionStatus.Approved),
            CreatePrice(otherMarket, otherUnionProduct, SubmissionStatus.Approved));
        await dbContext.SaveChangesAsync();
        var repository = new ProductRepository(dbContext);

        var products = await repository.GetOptionsAsync(unionOrWardId: selectedUnionId);

        products.Select(x => x.Id).Should().BeEquivalentTo([approvedProduct.Id, pendingProduct.Id]);
        products.Select(x => x.Id).Should().NotContain([rejectedProduct.Id, otherUnionProduct.Id]);
    }

    [Fact]
    public async Task ProductRepository_GetOptionsAsync_WithMarket_ReturnsProductsWithApprovedPricesInMarket()
    {
        await using var dbContext = CreateDbContext();
        var selectedMarket = CreateMarket("Selected Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 1");
        var otherMarket = CreateMarket("Other Bazar", "Dhaka", "Tangail", "Tangail Sadar", "Ward 2");
        var selectedProduct = CreateProduct("Potato", "Vegetables", true, RecordStatus.Approved);
        var pendingPriceProduct = CreateProduct("Pending Price Onion", "Vegetables", true, RecordStatus.Approved);
        var otherMarketProduct = CreateProduct("Other Market Fish", "Protein", true, RecordStatus.Approved);
        await dbContext.Markets.AddRangeAsync(selectedMarket, otherMarket);
        await dbContext.Products.AddRangeAsync(selectedProduct, pendingPriceProduct, otherMarketProduct);
        await dbContext.PriceSubmissions.AddRangeAsync(
            CreatePrice(selectedMarket, selectedProduct, SubmissionStatus.Approved),
            CreatePrice(selectedMarket, pendingPriceProduct, SubmissionStatus.Pending),
            CreatePrice(otherMarket, otherMarketProduct, SubmissionStatus.Approved));
        await dbContext.SaveChangesAsync();
        var repository = new ProductRepository(dbContext);

        var products = await repository.GetOptionsAsync(marketId: selectedMarket.Id);

        products.Should().ContainSingle();
        products[0].Id.Should().Be(selectedProduct.Id);
    }

    private static Market CreateMarket(
        string marketName,
        string divisionName,
        string districtName,
        string upazilaName,
        string? unionOrWardName,
        Guid? unionOrWardId = null,
        RecordStatus status = RecordStatus.Approved)
    {
        var finalUnionOrWardId = unionOrWardName is null ? (Guid?)null : unionOrWardId ?? Guid.NewGuid();
        var division = new Division { Id = Guid.NewGuid(), NameEn = divisionName, NameBn = divisionName };
        var district = new District { Id = Guid.NewGuid(), DivisionId = division.Id, NameEn = districtName, NameBn = districtName };
        var upazila = new Upazila { Id = Guid.NewGuid(), DistrictId = district.Id, NameEn = upazilaName, NameBn = upazilaName };
        var unionOrWard = finalUnionOrWardId.HasValue
            ? new UnionOrWard { Id = finalUnionOrWardId.Value, UpazilaId = upazila.Id, NameEn = unionOrWardName!, NameBn = unionOrWardName! }
            : null;

        return new Market
        {
            Id = Guid.NewGuid(),
            MarketName = marketName,
            Area = "Main area",
            VillageOrMoholla = string.Empty,
            Landmark = string.Empty,
            DivisionId = division.Id,
            Division = division,
            DistrictId = district.Id,
            District = district,
            UpazilaId = upazila.Id,
            Upazila = upazila,
            UnionOrWardId = finalUnionOrWardId,
            UnionOrWard = unionOrWard,
            Status = status
        };
    }

    private static Product CreateProduct(string productName, string categoryName, bool isActive, RecordStatus status)
    {
        var categoryId = Guid.NewGuid();

        return new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Category = new ProductCategory { Id = categoryId, NameEn = categoryName, NameBn = categoryName },
            NameEn = productName,
            NameBn = productName,
            PrimaryUnit = "kg",
            ProductState = ProductState.Fresh,
            Status = status,
            IsActive = isActive
        };
    }

    private static PriceSubmission CreatePrice(Market market, Product product, SubmissionStatus status)
    {
        return new PriceSubmission
        {
            Id = Guid.NewGuid(),
            MarketId = market.Id,
            Market = market,
            ProductId = product.Id,
            Product = product,
            Unit = product.PrimaryUnit,
            PricePerUnit = 50m,
            PriceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SellerType = SellerType.Retail,
            PriceSource = PriceSource.ObservedInMarket,
            QualityGrade = QualityGrade.Standard,
            Status = status
        };
    }

    private static BazarKotoDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BazarKotoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BazarKotoDbContext(options);
    }
}
