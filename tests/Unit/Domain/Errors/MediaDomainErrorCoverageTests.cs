using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Errors;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Unit.Domain.Errors;

[TestClass]
public class MediaDomainErrorCoverageTests
{
    [TestMethod]
    public void StoragePath_Create_WithEmpty_ReturnsEmptyError()
    {
        var result = StoragePath.Create("  ");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(StoragePathError.Empty.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void StoragePath_Create_WithTooLongPath_ReturnsTooLongError()
    {
        var result = StoragePath.Create(new string('a', StoragePath.MaxLength + 1));
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(StoragePathError.TooLong.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void StoragePath_Create_WithTraversal_ReturnsInvalidFormatError()
    {
        var result = StoragePath.Create("quarantine/../evil");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(StoragePathError.InvalidFormat.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void StoragePath_Create_WithValidPath_ReturnsSuccess()
    {
        var result = StoragePath.Create("quarantine/listing/abc/file.jpg");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("quarantine/listing/abc/file.jpg", result.Value!.Value);
    }

    [TestMethod]
    public void MediaFileSize_Create_WithNonPositive_ReturnsNonPositiveError()
    {
        var result = MediaFileSize.Create(0);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileSizeError.InvalidFormat.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void ScanAttemptCount_Create_WithNegative_ReturnsNegativeError()
    {
        var result = ScanAttemptCount.Create(-1);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ScanAttemptCountError.Negative.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void AccessLevel_TryParse_WithInvalidValue_ReturnsInvalidError()
    {
        var result = AccessLevelExtensions.TryParseAccessLevel("not-a-level");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(AccessLevelError.Invalid.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void MediaStatus_TryParse_WithInvalidValue_ReturnsInvalidError()
    {
        var result = MediaStatusExtensions.TryParseMediaStatus("not-a-status");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaStatusError.Invalid.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void OwnerType_TryParse_WithInvalidValue_ReturnsInvalidError()
    {
        var result = OwnerTypeExtensions.TryParseOwnerType("not-an-owner");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(OwnerTypeError.Invalid.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void ScanRejectionReason_TryParse_WithInvalidValue_ReturnsInvalidError()
    {
        var result = ScanRejectionReasonExtensions.TryParseScanRejectionReason("not-a-reason");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ScanRejectionReasonError.Invalid.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void VariantType_TryParse_WithInvalidValue_ReturnsInvalidError()
    {
        var result = VariantTypeExtensions.TryParseVariantType("not-a-type");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(VariantTypeError.Invalid.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void MediaUpload_BeginScan_WhenNotPending_ReturnsNotPendingError()
    {
        var upload = MediaUpload.Create(
            new MediaFileId(Guid.NewGuid()),
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            StoragePath.FromDatabase("quarantine/listing/owner/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;
        upload.BeginScan();

        var result = upload.BeginScan();
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaUploadError.NotPending.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void MediaUpload_Approve_WhenNotScanning_ReturnsNotScanningError()
    {
        var upload = MediaUpload.Create(
            new MediaFileId(Guid.NewGuid()),
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            StoragePath.FromDatabase("quarantine/listing/owner/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

        var result = upload.Approve();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaUploadError.NotScanning.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void MediaUpload_Reject_WhenNotScanning_ReturnsNotScanningError()
    {
        var upload = MediaUpload.Create(
            new MediaFileId(Guid.NewGuid()),
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            StoragePath.FromDatabase("quarantine/listing/owner/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

        var result = upload.Reject("infected");

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaUploadError.NotScanning.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void MediaFile_Create_FromScanningUpload_ShouldCreateVariantsAndRaiseEvent()
    {
        var upload = MediaUpload.Create(
            new MediaFileId(Guid.NewGuid()),
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            StoragePath.FromDatabase("quarantine/listing/owner/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;
        upload.BeginScan();

        var promotedVariants = new[]
        {
            new PromotedVariantInput(
                VariantType.Original,
                Link.FromDatabase("https://example.com/original.jpg"),
                null,
                MediaFileSize.FromDatabase(100)),
        };

        var result = MediaFile.Create(
            upload,
            AccessLevel.Public,
            promotedVariants);

        Assert.IsTrue(result.IsSuccess);
        var mediaFile = result.Value!;
        Assert.AreEqual("https://example.com/original.jpg", mediaFile.ResolvePublicUrl(AccessLevel.Public));
        Assert.AreEqual(1, mediaFile.Variants.Count);
        Assert.AreEqual(1, mediaFile.DomainEvents.Count);
        Assert.IsInstanceOfType<global::Media.Domain.Events.FileApproved>(mediaFile.DomainEvents.First());
    }
}
