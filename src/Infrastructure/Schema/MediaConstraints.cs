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

    public const string MediaFileOwnerTypeCheck = "CK_MediaFile_OwnerType";
    public const string MediaFileContentTypeCheck = "CK_MediaFile_ContentType";
    public const string MediaPolicyOwnerTypeCheck = "CK_MediaPolicy_OwnerType";
    public const string MediaPolicyAllowedContentTypesCheck = "CK_MediaPolicy_AllowedContentTypes";
    public const string MediaPolicyAccessLevelCheck = "CK_MediaPolicy_AccessLevel";
    public const string MediaUploadOwnerTypeCheck = "CK_MediaUpload_OwnerType";
    public const string MediaUploadStatusCheck = "CK_MediaUpload_Status";
    public const string MediaUploadContentTypeCheck = "CK_MediaUpload_ContentType";
    public const string MediaVariantVariantTypeCheck = "CK_MediaVariant_VariantType";

    /// <summary>[Flags] AllowedContentTypes: only defined bits (None..Pdf = 0..31) may be set.</summary>
    public const string AllowedContentTypesFlagsSql = "[AllowedContentTypes] >= 0 AND [AllowedContentTypes] <= 31";
}
