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
    public void StoragePath_FromQuarantineUpload_WithValidPath_ReturnsSuccess()
    {
        var result = StoragePath.FromQuarantineUpload("quarantine/listing/abc/file.jpg");
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
    public void MediaMaxCount_Create_WithNonPositive_ReturnsNonPositiveError()
    {
        var result = MediaMaxCount.Create(0);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaMaxCountError.Invalid.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void MediaMaxFileSize_Create_WithNonPositive_ReturnsNonPositiveError()
    {
        var result = MediaMaxFileSize.Create(-1);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaMaxFileSizeError.NonPositive.Code.Value, result.Error!.Code.Value);
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
    public void MediaType_TryParse_WithInvalidValue_ReturnsInvalidError()
    {
        var result = MediaTypeExtensions.TryParseMediaType("not-a-type");
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaTypeError.Invalid.Code.Value, result.Error!.Code.Value);
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
    public void MediaFile_BeginScan_WhenNotPending_ReturnsNotPendingError()
    {
        var mediaFile = MediaFile.Create(
            new MediaFileId(Guid.NewGuid()),
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            global::Media.Domain.Enums.Properties.MediaType.Photo,
            MediaStatus.Approved,
            StoragePath.FromDatabase("quarantine/listing/owner/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

        var result = mediaFile.BeginScan();
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.NotPending.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void MediaFile_Approve_WhenNotScanning_ReturnsNotScanningError()
    {
        var mediaFile = MediaFile.Create(
            new MediaFileId(Guid.NewGuid()),
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            global::Media.Domain.Enums.Properties.MediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase("quarantine/listing/owner/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

        var result = mediaFile.Approve(
            "https://example.com/file.jpg",
            AccessLevel.Public,
            []);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.NotScanning.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public void MediaFile_Approve_WhenScanning_ShouldCreateVariantsAndRaiseEvent()
    {
        var mediaFileId = new MediaFileId(Guid.NewGuid());
        var mediaFile = MediaFile.Create(
            mediaFileId,
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            global::Media.Domain.Enums.Properties.MediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase("quarantine/listing/owner/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;
        mediaFile.BeginScan();

        var promotedVariants = new[]
        {
            new PromotedVariantInput(
                VariantType.Original,
                VariantUrl.FromStorage("https://example.com/original.jpg"),
                null,
                null,
                MediaFileSize.FromDatabase(100)),
        };

        var result = mediaFile.Approve(
            "https://example.com/original.jpg",
            AccessLevel.Public,
            promotedVariants);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(MediaStatus.Approved, mediaFile.Status);
        Assert.AreEqual(1, mediaFile.Variants.Count);
        Assert.AreEqual(1, mediaFile.DomainEvents.Count);
        Assert.IsInstanceOfType<global::Media.Domain.Events.FileApproved>(mediaFile.DomainEvents.First());
    }
}
