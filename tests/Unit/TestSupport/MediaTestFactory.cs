using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Unit.TestSupport;

internal static class MediaTestFactory
{
    public static MediaUpload CreateUpload(
        MediaFileId? id = null,
        OwnerId? ownerId = null,
        OwnerType ownerType = OwnerType.Listing,
        string? quarantinePath = null,
        AllowedContentTypes contentType = AllowedContentTypes.Jpeg,
        long fileSizeBytes = 1024,
        UploadedByClientId? uploadedBy = null)
    {
        var mediaFileId = id ?? MediaFileId.New();
        var owner = ownerId ?? new OwnerId(Guid.NewGuid());
        return MediaUpload.Create(
            mediaFileId,
            owner,
            ownerType,
            StoragePath.FromDatabase(quarantinePath ?? $"quarantine/listing/{owner.Value}/{mediaFileId.Value}.jpg"),
            contentType,
            MediaFileSize.FromDatabase(fileSizeBytes),
            uploadedBy ?? UploadedByClientId.New()).Value!;
    }

    public static MediaUpload CreateScanningUpload(
        MediaFileId? id = null,
        OwnerId? ownerId = null,
        OwnerType ownerType = OwnerType.Listing,
        UploadedByClientId? uploadedBy = null)
    {
        var upload = CreateUpload(id, ownerId, ownerType, uploadedBy: uploadedBy);
        upload.BeginScan();
        return upload;
    }

    public static MediaFile CreateReadyMediaFile(
        MediaFileId? id = null,
        OwnerId? ownerId = null,
        OwnerType ownerType = OwnerType.Listing,
        AccessLevel accessLevel = AccessLevel.Public,
        string? originalUrl = "https://example.com/file.jpg",
        IReadOnlyList<PromotedVariantInput>? variants = null,
        UploadedByClientId? uploadedBy = null)
    {
        var upload = CreateScanningUpload(id, ownerId, ownerType, uploadedBy);
        var promoted = variants ??
        [
            new PromotedVariantInput(
                VariantType.Original,
                Link.FromDatabase(originalUrl ?? "https://example.com/original.jpg"),
                null,
                MediaFileSize.FromDatabase(1024)),
        ];

        return MediaFile.Create(upload, accessLevel, promoted).Value!;
    }
}
