using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Kernel.Results;

namespace Media.Infrastructure.Configurations;

public sealed class MediaPolicyConfiguration : IEntityTypeConfiguration<MediaPolicy>
{
    public void Configure(EntityTypeBuilder<MediaPolicy> builder)
    {
        builder.ToTable("MediaPolicy", t =>
        {
            t.HasCheckConstraint(MediaConstraints.MediaPolicyOwnerTypeCheck, EnumParse.CheckConstraintSql<OwnerType>("OwnerType"));
            // [Flags] combinations (e.g. 15, 19) are valid, so no IN-list check here.
            t.HasCheckConstraint(MediaConstraints.MediaPolicyAllowedContentTypesCheck, MediaConstraints.AllowedContentTypesFlagsSql);
            t.HasCheckConstraint(MediaConstraints.MediaPolicyAccessLevelCheck, EnumParse.CheckConstraintSql<AccessLevel>("AccessLevel"));
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => new MediaPolicyId(v))
            .HasColumnName("Id");

        builder.Property(e => e.OwnerType)
            .HasColumnName("OwnerType")
            .HasConversion(v => (int)v, v => EnumParse.FromDatabase<OwnerType>(v))
            .IsRequired();

        builder.Property(e => e.MaxFileSizeBytes)
            .HasConversion(v => v.Value, v => MediaMaxFileSize.FromDatabase(v))
            .HasColumnName("MaxFileSizeBytes")
            .IsRequired();

        // [Flags]: EnumParse.FromDatabase rejects valid combinations (e.g. Jpeg|Png),
        // so hydrate the trusted int directly; range is guarded by the CHECK above.
        builder.Property(e => e.AllowedContentTypes)
            .HasColumnName("AllowedContentTypes")
            .HasConversion(v => (int)v, v => (AllowedContentTypes)v)
            .IsRequired();

        builder.Property(e => e.MaxMediaCount)
            .HasConversion(v => v.Value, v => MediaMaxCount.FromDatabase(v))
            .HasColumnName("MaxMediaCount")
            .IsRequired();

        builder.Property(e => e.AccessLevel)
            .HasColumnName("AccessLevel")
            .HasConversion(v => (int)v, v => EnumParse.FromDatabase<AccessLevel>(v))
            .IsRequired();

        builder.Property(e => e.WatermarkEnabled)
            .HasColumnName("WatermarkEnabled")
            .IsRequired();

        builder.HasIndex(e => e.OwnerType)
            .IsUnique()
            .HasDatabaseName(MediaConstraints.MediaPolicyOwnerTypeUnique);
    }
}
