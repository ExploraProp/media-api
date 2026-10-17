using Media.Domain.Entities.Definitions;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Persistence;
using Media.Infrastructure.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Media.Infrastructure.Configurations;

public sealed class MediaVariantConfiguration : IEntityTypeConfiguration<MediaVariant>
{
    public void Configure(EntityTypeBuilder<MediaVariant> builder)
    {
        builder.ToTable("MediaVariant");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasConversion(v => v.Value, v => new MediaVariantId(v))
            .HasColumnName("Id");

        builder.Property(v => v.MediaFileId)
            .HasConversion(v => v.Value, v => new MediaFileId(v))
            .HasColumnName("MediaFileId")
            .IsRequired();

        builder.Property(v => v.VariantType)
            .HasColumnName("VariantType")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(v => v.Url)
            .HasConversion(v => v.Value, v => Link.FromDatabase(v))
            .HasColumnName("Url")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(v => v.Metadata)
            .HasConversion(
                v => MediaVariantMetadataJsonConversions.Serialize(v),
                v => MediaVariantMetadataJsonConversions.Deserialize(v))
            .HasColumnName("Metadata")
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.FileSizeBytes)
            .HasConversion(v => v.Value, v => MediaFileSize.FromDatabase(v))
            .HasColumnName("FileSizeBytes")
            .IsRequired();

        builder.HasOne<MediaFile>()
            .WithMany(f => f.Variants)
            .HasForeignKey(v => v.MediaFileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.MediaFileId)
            .HasDatabaseName(MediaConstraints.MediaVariantMediaFileIdIndex);

        builder.HasIndex(v => new { v.MediaFileId, v.VariantType })
            .IsUnique()
            .HasDatabaseName(MediaConstraints.MediaVariantMediaFileIdVariantTypeUnique);
    }
}
