using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(120);
        builder.Property(x => x.NameBn).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(140);
        builder.Property(x => x.BbsCode).HasMaxLength(32);
        builder.HasIndex(x => new { x.DivisionId, x.Slug }).IsUnique();
        builder.HasOne(x => x.Division)
            .WithMany(x => x.Districts)
            .HasForeignKey(x => x.DivisionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Upazilas)
            .WithOne(x => x.District)
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
