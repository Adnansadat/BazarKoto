using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class UpazilaConfiguration : IEntityTypeConfiguration<Upazila>
{
    public void Configure(EntityTypeBuilder<Upazila> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NameBn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(170);
        builder.Property(x => x.BbsCode).HasMaxLength(32);
        builder.HasIndex(x => new { x.DistrictId, x.Slug }).IsUnique();
        builder.HasOne(x => x.District)
            .WithMany(x => x.Upazilas)
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.UnionOrWards)
            .WithOne(x => x.Upazila)
            .HasForeignKey(x => x.UpazilaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
