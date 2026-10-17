using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Unit.TestSupport;
using Media.Domain.Enums.Properties;
using Media.Domain.Services.Errors;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Services;
using SharedKernel.Application.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
namespace Media.Unit.Infrastructure.Services;

[TestClass]
public class MediaPromotionServiceTests
{
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<IImageProcessingService> _imageProcessingService = new();
    private MediaPromotionService _service = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());
    private static readonly MediaFileId MediaFileId = MediaFileId.New();

    [TestInitialize]
    public void Setup()
    {
        _service = new MediaPromotionService(_storageService.Object, _imageProcessingService.Object);
    }

    private static MediaUpload CreateScanningUpload() =>
        MediaTestFactory.CreateScanningUpload(MediaFileId, OwnerId);

    private static MediaPolicy CreateListingPolicy(
        bool watermarkEnabled = true,
        AccessLevel accessLevel = AccessLevel.Public) =>
        MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            OwnerType.Listing,
            MediaMaxFileSize.FromDatabase(16 * 1024 * 1024),
            AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
            MediaMaxCount.FromDatabase(50),
            accessLevel,
            watermarkEnabled);

    private static IReadOnlyList<ProcessedVariant> CreateProcessedVariants() =>
    [
        new ProcessedVariant(VariantType.Original, new MemoryStream(new byte[10]), $"{MediaFileId.Value}.jpg", null, null, 10),
        new ProcessedVariant(VariantType.Thumbnail, new MemoryStream(new byte[10]), "thumbnail.webp", 200, 200, 10),
        new ProcessedVariant(VariantType.Medium, new MemoryStream(new byte[10]), "medium.webp", 800, 600, 10),
        new ProcessedVariant(VariantType.Large, new MemoryStream(new byte[10]), "large.webp", 1920, 1080, 10),
    ];

    private static MemoryStream CreateJpegStream()
    {
        var ms = new MemoryStream();
        using var image = new Image<Rgba32>(200, 200);
        image.SaveAsJpeg(ms);
        ms.Position = 0;
        return ms;
    }

    private void SetupHappyPath()
    {
        _storageService
            .Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Result<Stream>.Success(CreateJpegStream()));

        _imageProcessingService
            .Setup(s => s.GenerateVariantsAsync(
                It.IsAny<Stream>(), It.IsAny<MediaFileId>(), It.IsAny<string>(),
                It.IsAny<AllowedContentTypes>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<VariantType>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ProcessedVariant>>.Success(CreateProcessedVariants()));

        _storageService
            .Setup(s => s.UploadStreamToPathAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string path, string _, bool isPublic, CancellationToken _) =>
                Result<string>.Success(isPublic
                    ? $"https://firebase.storage/{path}"
                    : path));
    }

    [TestMethod]
    public async Task PromoteAsync_WithValidFile_ShouldReturnVariants()
    {
        SetupHappyPath();

        var result = await _service.PromoteAsync(CreateScanningUpload(), CreateListingPolicy(), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(4, result.Value!.Variants.Count);
        Assert.IsTrue(result.Value.IsPublic);
    }

    [TestMethod]
    public async Task PromoteAsync_WhenGenerateVariantsFails_ShouldPropagateError()
    {
        _storageService
            .Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Result<Stream>.Success(CreateJpegStream()));

        _storageService
            .Setup(s => s.UploadStreamToPathAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string path, string _, bool isPublic, CancellationToken _) =>
                Result<string>.Success(isPublic ? $"https://firebase.storage/{path}" : path));

        _imageProcessingService
            .Setup(s => s.GenerateVariantsAsync(
                It.IsAny<Stream>(), It.IsAny<MediaFileId>(), It.IsAny<string>(),
                It.IsAny<AllowedContentTypes>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<VariantType>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ProcessedVariant>>.Failure(
                ImageProcessingServiceError.Processing));

        var result = await _service.PromoteAsync(CreateScanningUpload(), CreateListingPolicy(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ImageProcessingServiceError.Processing.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task PromoteAsync_WhenVariantUploadFails_ShouldRollbackAlreadyUploadedVariants()
    {
        SetupHappyPath();

        _imageProcessingService
            .Setup(s => s.GenerateVariantsAsync(
                It.IsAny<Stream>(), It.IsAny<MediaFileId>(), It.IsAny<string>(),
                It.IsAny<AllowedContentTypes>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<VariantType>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ProcessedVariant>>.Success(new List<ProcessedVariant>
            {
                new(VariantType.Original, new MemoryStream(new byte[10]), $"{MediaFileId.Value}.jpg", null, null, 10),
                new(VariantType.Thumbnail, new MemoryStream(new byte[10]), "thumbnail.webp", 200, 200, 10),
                new(VariantType.Medium, new MemoryStream(new byte[10]), "medium.webp", 800, 600, 10),
            }));

        var uploadCallCount = 0;
        _storageService
            .Setup(s => s.UploadStreamToPathAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                uploadCallCount++;
                return uploadCallCount <= 2
                    ? Result<string>.Success($"https://firebase.storage/listing/variant{uploadCallCount}.webp")
                    : Result<string>.Failure(SharedFailures.SystemFailed);
            });

        _storageService
            .Setup(s => s.DeleteFileByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _service.PromoteAsync(CreateScanningUpload(), CreateListingPolicy(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        _storageService.Verify(
            s => s.DeleteFileByUrlAsync("https://firebase.storage/listing/variant2.webp"),
            Times.Once);
    }

    [TestMethod]
    public async Task PromoteAsync_WhenWatermarkEnabledAndPublicPolicy_ShouldPassApplyWatermarkTrue()
    {
        SetupHappyPath();

        await _service.PromoteAsync(
            CreateScanningUpload(),
            CreateListingPolicy(watermarkEnabled: true, accessLevel: AccessLevel.Public),
            CancellationToken.None);

        _imageProcessingService.Verify(s => s.GenerateVariantsAsync(
            It.IsAny<Stream>(), It.IsAny<MediaFileId>(), It.IsAny<string>(),
            It.IsAny<AllowedContentTypes>(), true, OwnerType.Listing.GetVariantsToGenerate(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task PromoteAsync_WhenWatermarkDisabled_ShouldPassApplyWatermarkFalse()
    {
        SetupHappyPath();

        await _service.PromoteAsync(
            CreateScanningUpload(),
            CreateListingPolicy(watermarkEnabled: false, accessLevel: AccessLevel.Public),
            CancellationToken.None);

        _imageProcessingService.Verify(s => s.GenerateVariantsAsync(
            It.IsAny<Stream>(), It.IsAny<MediaFileId>(), It.IsAny<string>(),
            It.IsAny<AllowedContentTypes>(), false, OwnerType.Listing.GetVariantsToGenerate(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task PromoteAsync_WhenAccessLevelPrivate_ShouldSkipVariantGenerationAndReturnOriginalOnly()
    {
        SetupHappyPath();

        var result = await _service.PromoteAsync(
            CreateScanningUpload(),
            CreateListingPolicy(watermarkEnabled: true, accessLevel: AccessLevel.Private),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.Value!.IsPublic);
        Assert.AreEqual(1, result.Value.Variants.Count);
        Assert.AreEqual(VariantType.Original, result.Value.Variants[0].VariantType);
        _imageProcessingService.Verify(s => s.GenerateVariantsAsync(
            It.IsAny<Stream>(), It.IsAny<MediaFileId>(), It.IsAny<string>(),
            It.IsAny<AllowedContentTypes>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<VariantType>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
