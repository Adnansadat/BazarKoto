namespace BazarKoto.Contracts.UserTracking;

public class UserTrackingInput
{
    public Guid? TrackingGuid { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public decimal? GpsAccuracyMeters { get; set; }
    public string? GpsPermissionStatus { get; set; }
    public string? IpBasedCountry { get; set; }
    public string? IpBasedRegion { get; set; }
    public string? IpBasedCity { get; set; }
    public decimal? IpBasedLatitude { get; set; }
    public decimal? IpBasedLongitude { get; set; }
    public string? IpLocationProvider { get; set; }
    public string? IpLocationAccuracy { get; set; }
    public Guid? LastKnownDivisionId { get; set; }
    public Guid? LastKnownDistrictId { get; set; }
    public Guid? LastKnownUpazilaId { get; set; }
    public Guid? LastKnownUnionOrWardId { get; set; }
    public string? LocationSource { get; set; }
}
