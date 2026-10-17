namespace Media.Infrastructure.Schema;

public static class MediaConstraints
{
    public const string MediaFileOwnerIdIndex = "IX_MediaFile_OwnerId";
    public const string MediaFileOwnerIdOwnerTypeIndex = "IX_MediaFile_OwnerId_OwnerType";
    public const string MediaUploadOwnerIdIndex = "IX_MediaUpload_OwnerId";
    public const string MediaUploadOwnerIdOwnerTypeIndex = "IX_MediaUpload_OwnerId_OwnerType";
    public const string MediaUploadStatusIndex = "IX_MediaUpload_Status";
    public const string MediaPolicyOwnerTypeUnique = "IX_MediaPolicy_OwnerType";
    public const string MediaVariantMediaFileIdIndex = "IX_MediaVariant_MediaFileId";
    public const string MediaVariantMediaFileIdVariantTypeUnique = "UX_MediaVariant_MediaFileId_VariantType";
    public const string MediaVariantMetadataWidthIndex = "IX_MediaVariant_MetadataWidth";
    public const string MediaVariantMetadataHeightIndex = "IX_MediaVariant_MetadataHeight";
}
