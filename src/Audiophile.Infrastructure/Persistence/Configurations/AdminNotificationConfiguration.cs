using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audiophile.Infrastructure.Persistence.Configurations;

internal sealed class AdminNotificationConfiguration : IEntityTypeConfiguration<AdminNotification>
{
    public void Configure(EntityTypeBuilder<AdminNotification> b)
    {
        b.Property(n => n.Kind).HasConversion<string>().HasMaxLength(30);
        b.Property(n => n.Title).HasMaxLength(AdminNotification.TitleMaxLength);
        b.Property(n => n.Message).HasMaxLength(AdminNotification.MessageMaxLength);

        // The bell reads the newest first and counts the unread ones
        b.HasIndex(n => n.CreatedAt);
        b.HasIndex(n => n.ReadAt).HasFilter("\"ReadAt\" IS NULL");
    }
}
