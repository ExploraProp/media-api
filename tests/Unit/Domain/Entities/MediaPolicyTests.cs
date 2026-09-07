using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;

namespace Media.Unit.Domain.Entities;

[TestClass]
public class MediaPolicyTests
{
    private static MediaPolicy CreatePolicy(
        long maxFileSizeBytes = 1024,
        AllowedContentTypes allowedContentTypes = AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
        int maxMediaCount = 10) =>
        MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            OwnerType.Listing,
            MediaMaxFileSize.FromDatabase(maxFileSizeBytes),
            allowedContentTypes,
            MediaMaxCount.FromDatabase(maxMediaCount),
            AccessLevel.Public,
            watermarkEnabled: false);

    [TestMethod]
    public void ValidateFileSize_WhenWithinLimit_ShouldSucceed()
    {
        var result = CreatePolicy().ValidateFileSize(512);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void ValidateFileSize_WhenExceedsLimit_ShouldReturnFileTooLarge()
    {
        var result = CreatePolicy(maxFileSizeBytes: 100).ValidateFileSize(101);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.TooLarge, result.Error!);
    }

    [TestMethod]
    public void ValidateContentType_WhenAllowed_ShouldReturnType()
    {
        var result = CreatePolicy().ValidateContentType(AllowedContentTypes.Jpeg);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(AllowedContentTypes.Jpeg, result.Value);
    }

    [TestMethod]
    public void ValidateContentType_WhenUnknown_ShouldReturnUnsupportedContentType()
    {
        var result = CreatePolicy().ValidateContentType(null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.UnsupportedContentType, result.Error!);
    }

    [TestMethod]
    public void ValidateContentType_WhenNotInPolicy_ShouldReturnUnsupportedContentType()
    {
        var result = CreatePolicy(allowedContentTypes: AllowedContentTypes.Jpeg).ValidateContentType(AllowedContentTypes.Pdf);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.UnsupportedContentType, result.Error!);
    }

    [TestMethod]
    public void ValidateMediaCount_WhenBelowLimit_ShouldSucceed()
    {
        var result = CreatePolicy(maxMediaCount: 10).ValidateMediaCount(9);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void ValidateMediaCount_WhenAtLimit_ShouldReturnMaxMediaCountReached()
    {
        var result = CreatePolicy(maxMediaCount: 10).ValidateMediaCount(10);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.TooMany, result.Error!);
    }
}
