using BazarKoto.Application.Interfaces;
using BazarKoto.Contracts.UserTracking;
using BazarKoto.Domain.Entities;

namespace BazarKoto.Application.Services;

public class UserTrackingService : IUserTrackingService
{
    private readonly IUserTrackingRepository _userTrackingRepository;
    private readonly IUserTrackingRequestContextAccessor _requestContextAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public UserTrackingService(
        IUserTrackingRepository userTrackingRepository,
        IUserTrackingRequestContextAccessor requestContextAccessor,
        IUnitOfWork unitOfWork)
    {
        _userTrackingRepository = userTrackingRepository;
        _requestContextAccessor = requestContextAccessor;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserTrackingResult> CreateOrUpdateAsync(UserTrackingInput? input = null, CancellationToken cancellationToken = default)
    {
        var entity = await CreateOrUpdateEntityAsync(input, cancellationToken);

        return new UserTrackingResult
        {
            UserTrackingDetailsId = entity.Id,
            TrackingGuid = entity.TrackingGuid
        };
    }

    public async Task<UserTrackingDetails> CreateOrUpdateEntityAsync(UserTrackingInput? input = null, CancellationToken cancellationToken = default)
    {
        input ??= new UserTrackingInput();
        var trackingGuid = input.TrackingGuid.GetValueOrDefault();

        if (trackingGuid == Guid.Empty)
        {
            trackingGuid = Guid.NewGuid();
        }

        var now = DateTime.UtcNow;
        var userTrackingDetails = await _userTrackingRepository.GetByTrackingGuidAsync(trackingGuid, cancellationToken);
        var isNew = userTrackingDetails is null;

        if (isNew)
        {
            userTrackingDetails = new UserTrackingDetails
            {
                TrackingGuid = trackingGuid,
                FirstSeenAt = now
            };

            await _userTrackingRepository.AddAsync(userTrackingDetails, cancellationToken);
        }

        ApplyRequestContext(userTrackingDetails!);
        ApplyUserAgentDetails(userTrackingDetails!);
        ApplyTrackingInput(userTrackingDetails!, input);
        userTrackingDetails!.LastSeenAt = now;

        if (!isNew)
        {
            _userTrackingRepository.Update(userTrackingDetails);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return userTrackingDetails;
    }

    private void ApplyRequestContext(UserTrackingDetails userTrackingDetails)
    {
        userTrackingDetails.RawIpAddress = NormalizeOptional(_requestContextAccessor.RawIpAddress);
        userTrackingDetails.RawUserAgent = NormalizeOptional(_requestContextAccessor.RawUserAgent);
    }

    private static void ApplyTrackingInput(UserTrackingDetails userTrackingDetails, UserTrackingInput input)
    {
        if (input.GpsLatitude.HasValue)
        {
            userTrackingDetails.GpsLatitude = input.GpsLatitude;
        }

        if (input.GpsLongitude.HasValue)
        {
            userTrackingDetails.GpsLongitude = input.GpsLongitude;
        }

        if (input.GpsAccuracyMeters.HasValue)
        {
            userTrackingDetails.GpsAccuracyMeters = input.GpsAccuracyMeters;
        }

        SetIfProvided(input.GpsPermissionStatus, value => userTrackingDetails.GpsPermissionStatus = value);
        SetIfProvided(input.IpBasedCountry, value => userTrackingDetails.IpBasedCountry = value);
        SetIfProvided(input.IpBasedRegion, value => userTrackingDetails.IpBasedRegion = value);
        SetIfProvided(input.IpBasedCity, value => userTrackingDetails.IpBasedCity = value);

        if (input.IpBasedLatitude.HasValue)
        {
            userTrackingDetails.IpBasedLatitude = input.IpBasedLatitude;
        }

        if (input.IpBasedLongitude.HasValue)
        {
            userTrackingDetails.IpBasedLongitude = input.IpBasedLongitude;
        }

        SetIfProvided(input.IpLocationProvider, value => userTrackingDetails.IpLocationProvider = value);
        SetIfProvided(input.IpLocationAccuracy, value => userTrackingDetails.IpLocationAccuracy = value);

        if (input.LastKnownDivisionId.HasValue)
        {
            userTrackingDetails.LastKnownDivisionId = input.LastKnownDivisionId;
        }

        if (input.LastKnownDistrictId.HasValue)
        {
            userTrackingDetails.LastKnownDistrictId = input.LastKnownDistrictId;
        }

        if (input.LastKnownUpazilaId.HasValue)
        {
            userTrackingDetails.LastKnownUpazilaId = input.LastKnownUpazilaId;
        }

        if (input.LastKnownUnionOrWardId.HasValue)
        {
            userTrackingDetails.LastKnownUnionOrWardId = input.LastKnownUnionOrWardId;
        }

        SetIfProvided(input.LocationSource, value => userTrackingDetails.LocationSource = value);
    }

    private static void ApplyUserAgentDetails(UserTrackingDetails userTrackingDetails)
    {
        var userAgent = userTrackingDetails.RawUserAgent;

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            userTrackingDetails.BrowserName = "Unknown";
            userTrackingDetails.BrowserVersion = null;
            userTrackingDetails.DeviceType = "Unknown";
            userTrackingDetails.OS = "Unknown";
            return;
        }

        userTrackingDetails.DeviceType = ParseDeviceType(userAgent);
        userTrackingDetails.OS = ParseOperatingSystem(userAgent);
        var browser = ParseBrowser(userAgent);
        userTrackingDetails.BrowserName = browser.Name;
        userTrackingDetails.BrowserVersion = browser.Version;
    }

    private static string ParseDeviceType(string userAgent)
    {
        if (ContainsAny(userAgent, "iPad", "Tablet"))
        {
            return "Tablet";
        }

        if (ContainsAny(userAgent, "Mobile", "Android", "iPhone"))
        {
            return "Mobile";
        }

        return "Desktop";
    }

    private static string ParseOperatingSystem(string userAgent)
    {
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Android";
        }

        if (ContainsAny(userAgent, "iPhone", "iPad", "iPod"))
        {
            return "iOS";
        }

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            return "Windows";
        }

        if (ContainsAny(userAgent, "Macintosh", "Mac OS X"))
        {
            return "macOS";
        }

        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            return "Linux";
        }

        return "Unknown";
    }

    private static (string Name, string? Version) ParseBrowser(string userAgent)
    {
        if (userAgent.Contains("SamsungBrowser/", StringComparison.OrdinalIgnoreCase))
        {
            return ("Samsung Internet", ExtractVersion(userAgent, "SamsungBrowser/"));
        }

        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            return ("Edge", ExtractVersion(userAgent, "Edg/"));
        }

        if (ContainsAny(userAgent, "OPR/", "Opera/"))
        {
            return ("Opera", ExtractVersion(userAgent, "OPR/") ?? ExtractVersion(userAgent, "Opera/"));
        }

        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
        {
            return ("Firefox", ExtractVersion(userAgent, "Firefox/"));
        }

        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
        {
            return ("Chrome", ExtractVersion(userAgent, "Chrome/"));
        }

        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase))
        {
            return ("Safari", ExtractVersion(userAgent, "Version/") ?? ExtractVersion(userAgent, "Safari/"));
        }

        return ("Unknown", null);
    }

    private static string? ExtractVersion(string userAgent, string token)
    {
        var start = userAgent.IndexOf(token, StringComparison.OrdinalIgnoreCase);

        if (start < 0)
        {
            return null;
        }

        start += token.Length;
        var end = start;

        while (end < userAgent.Length && (char.IsDigit(userAgent[end]) || userAgent[end] == '.'))
        {
            end++;
        }

        return end > start ? userAgent[start..end] : null;
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void SetIfProvided(string? value, Action<string> assign)
    {
        var normalized = NormalizeOptional(value);

        if (normalized is not null)
        {
            assign(normalized);
        }
    }
}
