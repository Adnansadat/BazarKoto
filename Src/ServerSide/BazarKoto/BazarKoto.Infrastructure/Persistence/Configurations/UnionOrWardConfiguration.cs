using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class UnionOrWardConfiguration : IEntityTypeConfiguration<UnionOrWard>
{
    public void Configure(EntityTypeBuilder<UnionOrWard> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NameBn).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(180);
        builder.Property(x => x.BbsCode).HasMaxLength(32);
        builder.Property(x => x.Type).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.HasIndex(x => new { x.UpazilaId, x.Slug }).IsUnique();
        builder.HasOne(x => x.Upazila)
            .WithMany(x => x.UnionOrWards)
            .HasForeignKey(x => x.UpazilaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
