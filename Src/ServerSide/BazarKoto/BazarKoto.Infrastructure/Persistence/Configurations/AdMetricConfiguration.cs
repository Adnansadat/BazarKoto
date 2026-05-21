using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class AdMetricConfiguration : IEntityTypeConfiguration<AdMetric>
{
    public void Configure(EntityTypeBuilder<AdMetric> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PagePath).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Ctr).HasPrecision(9, 4);
        builder.HasIndex(x => x.RecordedAt);
    }
}
