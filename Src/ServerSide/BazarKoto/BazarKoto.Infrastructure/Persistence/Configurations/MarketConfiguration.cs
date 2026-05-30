using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Area).IsRequired().HasMaxLength(150);
        builder.Property(x => x.MarketName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.VillageOrMoholla).HasMaxLength(150);
        builder.Property(x => x.Landmark).HasMaxLength(250);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.MarketType).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(x => x.OperatingSchedule).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.DivisionId);
        builder.HasIndex(x => x.DistrictId);
        builder.HasIndex(x => x.UpazilaId);
        builder.HasIndex(x => x.UnionOrWardId);
        builder.HasIndex(x => x.MarketName);
        builder.HasIndex(x => new { x.DistrictId, x.UpazilaId, x.MarketName });
        builder.HasOne(x => x.Division)
            .WithMany()
            .HasForeignKey(x => x.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.District)
            .WithMany()
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Upazila)
            .WithMany()
            .HasForeignKey(x => x.UpazilaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UnionOrWard)
            .WithMany()
            .HasForeignKey(x => x.UnionOrWardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
