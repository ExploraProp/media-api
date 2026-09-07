using Media.Domain.Entities.Definitions;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Infrastructure.Configurations;

public sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("MediaFile");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => new MediaFileId(v))
            .HasColumnName("Id");

        builder.Property(e => e.OwnerId)
            .HasConversion(v => v.Value, v => new OwnerId(v))
            .HasColumnName("OwnerId")
            .IsRequired();

        builder.Property(e => e.OwnerType)
            .HasColumnName("OwnerType")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.MediaType)
            .HasColumnName("MediaType")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.QuarantinePath)
            .HasConversion(v => v.Value, v => StoragePath.FromDatabase(v))
            .HasColumnName("QuarantinePath")
            .HasMaxLength(StoragePath.MaxLength)
            .IsRequired();

        builder.Property(e => e.PublicUrl)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? Link.FromDatabase(v) : null)
            .HasColumnName("PublicUrl")
            .HasMaxLength(1024);

        builder.Property(e => e.AccessLevel)
            .HasColumnName("AccessLevel")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.ContentType)
            .HasColumnName("ContentType")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.FileSizeBytes)
            .HasConversion(v => v.Value, v => MediaFileSize.FromDatabase(v))
            .HasColumnName("FileSizeBytes")
            .IsRequired();

        builder.Property(e => e.UploadedAt)
            .HasColumnName("UploadedAt")
            .IsRequired();

        builder.Property(e => e.ApprovedAt)
            .HasColumnName("ApprovedAt");

        builder.Property(e => e.ScanAttempts)
            .HasConversion(v => v.Value, v => ScanAttemptCount.FromDatabase(v))
            .HasColumnName("ScanAttempts")
            .IsRequired();

        builder.Property(e => e.UploadedByClientUserId)
            .HasConversion(v => v.Value, v => new UploadedByClientId(v))
            .HasColumnName("UploadedByClientUserId")
            .IsRequired();

        builder.Navigation(e => e.Variants)
            .HasField("_variants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.OwnerId)
            .HasDatabaseName(MediaConstraints.MediaFileOwnerIdIndex);

        builder.HasIndex(e => new { e.OwnerId, e.OwnerType })
            .HasDatabaseName(MediaConstraints.MediaFileOwnerIdOwnerTypeIndex);

        builder.HasIndex(e => e.Status)
            .HasDatabaseName(MediaConstraints.MediaFileStatusIndex);
    }
}
