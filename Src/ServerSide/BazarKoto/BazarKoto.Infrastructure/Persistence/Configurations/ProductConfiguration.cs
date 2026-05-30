using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(x => x.NameBn).IsRequired().HasMaxLength(200);
        builder.Property(x => x.LocalName).HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.Property(x => x.PrimaryUnit).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProductState).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.NameEn);
        builder.HasIndex(x => x.NameBn);
        builder.HasIndex(x => x.LocalName);
        builder.HasIndex(x => new { x.CategoryId, x.Slug }).IsUnique();
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
