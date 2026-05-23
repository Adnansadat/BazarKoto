using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Services;
using BazarKoto.Contracts.Prices;
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
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task SubmitPriceAsync_WithExistingMarketAndProduct_SavesAllSubmittedFields()
    {
        var marketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        PriceSubmission? savedPrice = null;

        _marketRepository.Setup(x => x.GetByIdAsync(marketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Market { Id = marketId, MarketName = "Test Market" });
        _productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = productId, NameEn = "Potato", NameBn = "আলু" });
        _priceRepository.Setup(x => x.GetAsync(null, null, null, null, marketId, null, productId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _priceRepository.Setup(x => x.AddAsync(It.IsAny<PriceSubmission>(), It.IsAny<CancellationToken>()))
            .Callback<PriceSubmission, CancellationToken>((price, _) => savedPrice = price)
            .Returns(Task.CompletedTask);

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
        savedPrice.Unit.Should().Be("kg");
        savedPrice.PricePerUnit.Should().Be(55m);
        savedPrice.QuantityChecked.Should().Be(2m);
        savedPrice.SellerType.Should().Be(SellerType.Retail);
        savedPrice.PriceSource.Should().Be(PriceSource.ObservedInMarket);
        savedPrice.QualityGrade.Should().Be(QualityGrade.Premium);
        savedPrice.Notes.Should().Be("Clean, medium size");
        savedPrice.Status.Should().Be(SubmissionStatus.Pending);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        _priceRepository.Verify(x => x.Update(price), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private PriceService CreateService()
    {
        return new PriceService(
            _priceRepository.Object,
            _marketRepository.Object,
            _productRepository.Object,
            _priceSummaryService.Object,
            _unitOfWork.Object);
    }
}
