using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.ScreenshotUrl).HasMaxLength(512);
        builder.Property(x => x.ScreenshotFileName).HasMaxLength(160);
        builder.Property(x => x.ScreenshotOriginalFileName).HasMaxLength(255);
        builder.Property(x => x.ScreenshotContentType).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(x => x.AdminNote).HasMaxLength(2000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.BrowserName).HasMaxLength(80);
        builder.Property(x => x.DeviceType).HasMaxLength(80);
        builder.Property(x => x.OS).HasMaxLength(80);
    }
}
