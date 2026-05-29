namespace BazarKoto.Contracts.Prices;

public class SubmitPriceRequest
{
    public Guid MarketId { get; set; }
    public Guid ProductId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal PricePerUnit { get; set; }
    public decimal? QuantityChecked { get; set; }
    public DateOnly PriceDate { get; set; }
    public TimeOnly? PriceTime { get; set; }
    public string SellerType { get; set; } = string.Empty;
    public string PriceSource { get; set; } = string.Empty;
    public string QualityGrade { get; set; } = string.Empty;
    public string? Notes { get; set; }
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
    public string? LocationSource { get; set; }
}
