using BazarKoto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BazarKoto.Infrastructure.Persistence.Configurations;

public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(150);
        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.EntityId).HasMaxLength(128);
        builder.Property(x => x.OldValueJson).HasMaxLength(4000);
        builder.Property(x => x.NewValueJson).HasMaxLength(4000);
        builder.Property(x => x.IpHash).HasMaxLength(256);
        builder.HasOne(x => x.AdminUser)
            .WithMany()
            .HasForeignKey(x => x.AdminUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
