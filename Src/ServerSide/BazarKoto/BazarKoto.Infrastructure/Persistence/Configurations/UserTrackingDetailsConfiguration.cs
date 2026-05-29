using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class UserTrackingDetailsConfiguration : IEntityTypeConfiguration<UserTrackingDetails>
{
    public void Configure(EntityTypeBuilder<UserTrackingDetails> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RawIpAddress).HasMaxLength(128);
        builder.Property(x => x.RawUserAgent).HasMaxLength(1000);
        builder.Property(x => x.BrowserName).HasMaxLength(100);
        builder.Property(x => x.BrowserVersion).HasMaxLength(100);
        builder.Property(x => x.DeviceType).HasMaxLength(50);
        builder.Property(x => x.OS).HasMaxLength(100);
        builder.Property(x => x.GpsLatitude).HasPrecision(9, 6);
        builder.Property(x => x.GpsLongitude).HasPrecision(9, 6);
        builder.Property(x => x.GpsAccuracyMeters).HasPrecision(18, 2);
        builder.Property(x => x.GpsPermissionStatus).HasMaxLength(50);
        builder.Property(x => x.IpBasedCountry).HasMaxLength(100);
        builder.Property(x => x.IpBasedRegion).HasMaxLength(150);
        builder.Property(x => x.IpBasedCity).HasMaxLength(150);
        builder.Property(x => x.IpBasedLatitude).HasPrecision(9, 6);
        builder.Property(x => x.IpBasedLongitude).HasPrecision(9, 6);
        builder.Property(x => x.IpLocationProvider).HasMaxLength(150);
        builder.Property(x => x.IpLocationAccuracy).HasMaxLength(100);
        builder.Property(x => x.LocationSource).HasMaxLength(50);

        builder.HasIndex(x => x.TrackingGuid).IsUnique();
        builder.HasIndex(x => x.LastSeenAt);
        builder.HasIndex(x => x.LastKnownUnionOrWardId);
        builder.HasIndex(x => x.DeviceType);
        builder.HasIndex(x => x.OS);

        builder.HasOne(x => x.LastKnownDivision)
            .WithMany()
            .HasForeignKey(x => x.LastKnownDivisionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LastKnownDistrict)
            .WithMany()
            .HasForeignKey(x => x.LastKnownDistrictId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LastKnownUpazila)
            .WithMany()
            .HasForeignKey(x => x.LastKnownUpazilaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LastKnownUnionOrWard)
            .WithMany()
            .HasForeignKey(x => x.LastKnownUnionOrWardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
