using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class DailyPriceSummaryConfiguration : IEntityTypeConfiguration<DailyPriceSummary>
{
    public void Configure(EntityTypeBuilder<DailyPriceSummary> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MinPrice).HasPrecision(18, 2);
        builder.Property(x => x.MaxPrice).HasPrecision(18, 2);
        builder.Property(x => x.AveragePrice).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.ProductId, x.PriceDate });
        builder.HasIndex(x => new { x.DistrictId, x.ProductId, x.PriceDate });
        builder.HasIndex(x => new { x.UpazilaId, x.ProductId, x.PriceDate });
        builder.HasIndex(x => new { x.MarketId, x.ProductId, x.PriceDate });

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Market)
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

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
