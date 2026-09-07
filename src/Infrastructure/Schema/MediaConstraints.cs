namespace Media.Infrastructure.Schema;

public static class MediaConstraints
{
    public const string MediaFileOwnerIdIndex = "IX_MediaFile_OwnerId";
    public const string MediaFileOwnerIdOwnerTypeIndex = "IX_MediaFile_OwnerId_OwnerType";
    public const string MediaFileStatusIndex = "IX_MediaFile_Status";
    public const string MediaPolicyOwnerTypeUnique = "IX_MediaPolicy_OwnerType";
    public const string MediaVariantMediaFileIdIndex = "IX_MediaVariant_MediaFileId";
    public const string MediaVariantMediaFileIdVariantTypeUnique = "UX_MediaVariant_MediaFileId_VariantType";
}
