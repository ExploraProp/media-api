using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Kernel.Results;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Infrastructure.Configurations;

public sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("MediaFile", t =>
        {
            t.HasCheckConstraint(MediaConstraints.MediaFileOwnerTypeCheck, EnumParse.CheckConstraintSql<OwnerType>("OwnerType"));
            t.HasCheckConstraint(MediaConstraints.MediaFileContentTypeCheck, EnumParse.CheckConstraintSql<AllowedContentTypes>("ContentType"));
        });

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
            .HasConversion(v => (int)v, v => EnumParse.FromDatabase<OwnerType>(v))
            .IsRequired();

        builder.Property(e => e.ContentType)
            .HasColumnName("ContentType")
            .HasConversion(v => (int)v, v => EnumParse.FromDatabase<AllowedContentTypes>(v))
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
    }
}
