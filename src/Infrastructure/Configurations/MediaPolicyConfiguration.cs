using Media.Domain.Entities.Definitions;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Media.Infrastructure.Configurations;

public sealed class MediaPolicyConfiguration : IEntityTypeConfiguration<MediaPolicy>
{
    public void Configure(EntityTypeBuilder<MediaPolicy> builder)
    {
        builder.ToTable("MediaPolicy");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => new MediaPolicyId(v))
            .HasColumnName("Id");

        builder.Property(e => e.OwnerType)
            .HasColumnName("OwnerType")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.MaxFileSizeBytes)
            .HasConversion(v => v.Value, v => MediaMaxFileSize.FromDatabase(v))
            .HasColumnName("MaxFileSizeBytes")
            .IsRequired();

        builder.Property(e => e.AllowedContentTypes)
            .HasColumnName("AllowedContentTypes")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.MaxMediaCount)
            .HasConversion(v => v.Value, v => MediaMaxCount.FromDatabase(v))
            .HasColumnName("MaxMediaCount")
            .IsRequired();

        builder.Property(e => e.AccessLevel)
            .HasColumnName("AccessLevel")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.WatermarkEnabled)
            .HasColumnName("WatermarkEnabled")
            .IsRequired();

        builder.HasIndex(e => e.OwnerType)
            .IsUnique()
            .HasDatabaseName(MediaConstraints.MediaPolicyOwnerTypeUnique);
    }
}
