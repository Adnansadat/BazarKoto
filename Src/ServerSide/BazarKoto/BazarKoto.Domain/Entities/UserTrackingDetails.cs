using System;
using BazarKoto.Domain.Common;

namespace BazarKoto.Domain.Entities
{
    public class UserTrackingDetails : AuditableEntity
    {
        public Guid TrackingGuid { get; set; }
        public string? RawIpAddress { get; set; }
        public string? RawUserAgent { get; set; }
        public string? BrowserName { get; set; }
        public string? BrowserVersion { get; set; }
        public string? DeviceType { get; set; }
        public string? OS { get; set; }
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
        public Division? LastKnownDivision { get; set; }

        public Guid? LastKnownDistrictId { get; set; }
        public District? LastKnownDistrict { get; set; }

        public Guid? LastKnownUpazilaId { get; set; }
        public Upazila? LastKnownUpazila { get; set; }

        public Guid? LastKnownUnionOrWardId { get; set; }
        public UnionOrWard? LastKnownUnionOrWard { get; set; }

        public string? LocationSource { get; set; }
        public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    }
}
