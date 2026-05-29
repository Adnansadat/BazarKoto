using BazarKoto.Application.Interfaces;
using BazarKoto.Application.Services;
using BazarKoto.Contracts.UserTracking;
using BazarKoto.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BazarKoto.Tests;

public class UserTrackingServiceTests
{
    private readonly Mock<IUserTrackingRepository> _userTrackingRepository = new();
    private readonly Mock<IUserTrackingRequestContextAccessor> _requestContextAccessor = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task CreateOrUpdateAsync_WithMissingTrackingGuid_CreatesNewTrackingRow()
    {
        UserTrackingDetails? savedTrackingDetails = null;
        _requestContextAccessor.Setup(x => x.RawIpAddress).Returns("203.0.113.10");
        _requestContextAccessor.Setup(x => x.RawUserAgent).Returns((string?)null);
        _userTrackingRepository.Setup(x => x.AddAsync(It.IsAny<UserTrackingDetails>(), It.IsAny<CancellationToken>()))
            .Callback<UserTrackingDetails, CancellationToken>((details, _) => savedTrackingDetails = details)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.CreateOrUpdateAsync();

        result.TrackingGuid.Should().NotBe(Guid.Empty);
        savedTrackingDetails.Should().NotBeNull();
        savedTrackingDetails!.TrackingGuid.Should().Be(result.TrackingGuid);
        savedTrackingDetails.RawIpAddress.Should().Be("203.0.113.10");
        savedTrackingDetails.DeviceType.Should().Be("Unknown");
        savedTrackingDetails.OS.Should().Be("Unknown");
        savedTrackingDetails.BrowserName.Should().Be("Unknown");
        _userTrackingRepository.Verify(x => x.AddAsync(It.IsAny<UserTrackingDetails>(), It.IsAny<CancellationToken>()), Times.Once);
        _userTrackingRepository.Verify(x => x.Update(It.IsAny<UserTrackingDetails>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithExistingTrackingGuid_UpdatesLastSeenWithoutCreatingDuplicate()
    {
        var trackingGuid = Guid.NewGuid();
        var existing = new UserTrackingDetails
        {
            Id = Guid.NewGuid(),
            TrackingGuid = trackingGuid,
            FirstSeenAt = DateTime.UtcNow.AddDays(-3),
            LastSeenAt = DateTime.UtcNow.AddDays(-2),
            GpsLatitude = 23.750000m
        };
        var previousLastSeenAt = existing.LastSeenAt;

        _userTrackingRepository.Setup(x => x.GetByTrackingGuidAsync(trackingGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _requestContextAccessor.Setup(x => x.RawIpAddress).Returns("198.51.100.11");
        _requestContextAccessor.Setup(x => x.RawUserAgent).Returns("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125.0.0.0 Safari/537.36");

        var service = CreateService();

        var result = await service.CreateOrUpdateAsync(new UserTrackingInput { TrackingGuid = trackingGuid });

        result.UserTrackingDetailsId.Should().Be(existing.Id);
        result.TrackingGuid.Should().Be(trackingGuid);
        existing.LastSeenAt.Should().BeAfter(previousLastSeenAt);
        existing.RawIpAddress.Should().Be("198.51.100.11");
        existing.DeviceType.Should().Be("Desktop");
        existing.OS.Should().Be("Windows");
        existing.BrowserName.Should().Be("Chrome");
        existing.BrowserVersion.Should().Be("125.0.0.0");
        existing.GpsLatitude.Should().Be(23.750000m);
        _userTrackingRepository.Verify(x => x.AddAsync(It.IsAny<UserTrackingDetails>(), It.IsAny<CancellationToken>()), Times.Never);
        _userTrackingRepository.Verify(x => x.Update(existing), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithAndroidChromeUserAgent_ParsesMobileAndroidChrome()
    {
        UserTrackingDetails? savedTrackingDetails = null;
        _requestContextAccessor.Setup(x => x.RawUserAgent)
            .Returns("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 Chrome/126.0.6478.110 Mobile Safari/537.36");
        _userTrackingRepository.Setup(x => x.AddAsync(It.IsAny<UserTrackingDetails>(), It.IsAny<CancellationToken>()))
            .Callback<UserTrackingDetails, CancellationToken>((details, _) => savedTrackingDetails = details)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.CreateOrUpdateAsync();

        savedTrackingDetails.Should().NotBeNull();
        savedTrackingDetails!.DeviceType.Should().Be("Mobile");
        savedTrackingDetails.OS.Should().Be("Android");
        savedTrackingDetails.BrowserName.Should().Be("Chrome");
        savedTrackingDetails.BrowserVersion.Should().Be("126.0.6478.110");
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithEmptyUserAgent_DoesNotThrow()
    {
        UserTrackingDetails? savedTrackingDetails = null;
        _requestContextAccessor.Setup(x => x.RawUserAgent).Returns("");
        _userTrackingRepository.Setup(x => x.AddAsync(It.IsAny<UserTrackingDetails>(), It.IsAny<CancellationToken>()))
            .Callback<UserTrackingDetails, CancellationToken>((details, _) => savedTrackingDetails = details)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var action = async () => await service.CreateOrUpdateAsync();

        await action.Should().NotThrowAsync();
        savedTrackingDetails.Should().NotBeNull();
        savedTrackingDetails!.DeviceType.Should().Be("Unknown");
        savedTrackingDetails.OS.Should().Be("Unknown");
        savedTrackingDetails.BrowserName.Should().Be("Unknown");
        savedTrackingDetails.BrowserVersion.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithLocationFields_SavesProvidedLocationAndGpsFields()
    {
        UserTrackingDetails? savedTrackingDetails = null;
        var divisionId = Guid.NewGuid();
        var districtId = Guid.NewGuid();
        var upazilaId = Guid.NewGuid();
        var unionOrWardId = Guid.NewGuid();
        _userTrackingRepository.Setup(x => x.AddAsync(It.IsAny<UserTrackingDetails>(), It.IsAny<CancellationToken>()))
            .Callback<UserTrackingDetails, CancellationToken>((details, _) => savedTrackingDetails = details)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.CreateOrUpdateAsync(new UserTrackingInput
        {
            GpsLatitude = 23.810331m,
            GpsLongitude = 90.412521m,
            GpsAccuracyMeters = 24.5m,
            GpsPermissionStatus = "granted",
            IpBasedCountry = "Bangladesh",
            IpBasedRegion = "Dhaka",
            IpBasedCity = "Dhaka",
            IpBasedLatitude = 23.800000m,
            IpBasedLongitude = 90.400000m,
            IpLocationProvider = "Test Provider",
            IpLocationAccuracy = "Approximate",
            LastKnownDivisionId = divisionId,
            LastKnownDistrictId = districtId,
            LastKnownUpazilaId = upazilaId,
            LastKnownUnionOrWardId = unionOrWardId,
            LocationSource = "manual"
        });

        savedTrackingDetails.Should().NotBeNull();
        savedTrackingDetails!.GpsLatitude.Should().Be(23.810331m);
        savedTrackingDetails.GpsLongitude.Should().Be(90.412521m);
        savedTrackingDetails.GpsAccuracyMeters.Should().Be(24.5m);
        savedTrackingDetails.GpsPermissionStatus.Should().Be("granted");
        savedTrackingDetails.IpBasedCountry.Should().Be("Bangladesh");
        savedTrackingDetails.IpBasedRegion.Should().Be("Dhaka");
        savedTrackingDetails.IpBasedCity.Should().Be("Dhaka");
        savedTrackingDetails.IpBasedLatitude.Should().Be(23.800000m);
        savedTrackingDetails.IpBasedLongitude.Should().Be(90.400000m);
        savedTrackingDetails.IpLocationProvider.Should().Be("Test Provider");
        savedTrackingDetails.IpLocationAccuracy.Should().Be("Approximate");
        savedTrackingDetails.LastKnownDivisionId.Should().Be(divisionId);
        savedTrackingDetails.LastKnownDistrictId.Should().Be(districtId);
        savedTrackingDetails.LastKnownUpazilaId.Should().Be(upazilaId);
        savedTrackingDetails.LastKnownUnionOrWardId.Should().Be(unionOrWardId);
        savedTrackingDetails.LocationSource.Should().Be("manual");
    }

    private UserTrackingService CreateService()
    {
        return new UserTrackingService(
            _userTrackingRepository.Object,
            _requestContextAccessor.Object,
            _unitOfWork.Object);
    }
}
