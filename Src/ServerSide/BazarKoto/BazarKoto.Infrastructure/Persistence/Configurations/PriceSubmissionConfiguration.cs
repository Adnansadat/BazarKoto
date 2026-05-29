using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class PriceSubmissionConfiguration : IEntityTypeConfiguration<PriceSubmission>
{
    public void Configure(EntityTypeBuilder<PriceSubmission> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PricePerUnit).HasPrecision(18, 2);
        builder.Property(x => x.QuantityChecked).HasPrecision(18, 2);
        builder.Property(x => x.SellerType).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(x => x.PriceSource).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(x => x.QualityGrade).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => x.PriceDate);
        builder.HasIndex(x => new { x.UnionOrWardId, x.ProductId, x.PriceDate });
        builder.HasIndex(x => new { x.UnionOrWardId, x.MarketId, x.PriceDate });
        builder.HasIndex(x => new { x.MarketId, x.ProductId, x.PriceDate });
        builder.HasIndex(x => x.UserTrackingDetailsId);
        builder.HasIndex(x => x.TrackingGuid);
        builder.HasOne(x => x.Market)
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
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
        builder.HasOne(x => x.SubmittedByUser)
            .WithMany()
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.UserTrackingDetails)
            .WithMany()
            .HasForeignKey(x => x.UserTrackingDetailsId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
