using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class PageVisitConfiguration : IEntityTypeConfiguration<PageVisit>
{
    public void Configure(EntityTypeBuilder<PageVisit> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Path).IsRequired().HasMaxLength(500);
        builder.Property(x => x.PageTitle).HasMaxLength(250);
        builder.Property(x => x.VisitorId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.IpHash).HasMaxLength(256);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.Referrer).HasMaxLength(1000);
        builder.Property(x => x.DeviceType).HasMaxLength(100);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.HasIndex(x => x.VisitedAt);
        builder.HasIndex(x => x.VisitorId);
    }
}
