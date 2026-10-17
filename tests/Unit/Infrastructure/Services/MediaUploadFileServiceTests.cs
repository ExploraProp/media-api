using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Services;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Unit.Infrastructure.Services;

[TestClass]
public class MediaUploadFileServiceTests
{
    private readonly Mock<IContentTypeDetectionService> _contentTypeDetectionService = new();
    private readonly Mock<IStorageService> _storageService = new();
    private MediaUploadFileService _service = null!;

    [TestInitialize]
    public void Setup() =>
        _service = new MediaUploadFileService(_contentTypeDetectionService.Object, _storageService.Object);

    private static MediaPolicy CreatePolicy() =>
        MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            OwnerType.Listing,
            MediaMaxFileSize.FromDatabase(16 * 1024 * 1024),
            AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
            MediaMaxCount.FromDatabase(50),
            AccessLevel.Public,
            watermarkEnabled: true);

    private static IFormFile CreateFile(long length, byte[] header)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(header));
        return file.Object;
    }

    [TestMethod]
    public async Task PrepareAsync_WhenFileTooLarge_ShouldReturnFileTooLargeError()
    {
        var result = await _service.PrepareAsync(
            CreateFile(20 * 1024 * 1024, [0xFF, 0xD8, 0xFF]),
            CreatePolicy(),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.TooLarge, result.Error!);
    }

    [TestMethod]
    public async Task PrepareAsync_WhenContentTypeUnknown_ShouldReturnUnsupportedContentTypeError()
    {
        _contentTypeDetectionService
            .Setup(s => s.DetectAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AllowedContentTypes?)null);

        var result = await _service.PrepareAsync(
            CreateFile(1024, [0x4D, 0x5A]),
            CreatePolicy(),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.UnsupportedContentType, result.Error!);
    }

    [TestMethod]
    public async Task PrepareAsync_WhenValidJpeg_ShouldReturnStreamAndContentType()
    {
        _contentTypeDetectionService
            .Setup(s => s.DetectAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllowedContentTypes.Jpeg);

        await using var prepared = (await _service.PrepareAsync(
            CreateFile(1024, [0xFF, 0xD8, 0xFF, 0xE0]),
            CreatePolicy(),
            CancellationToken.None)).Value!;

        Assert.AreEqual(AllowedContentTypes.Jpeg, prepared.ContentType);
        Assert.IsTrue(prepared.Stream.CanRead);
    }

    [TestMethod]
    public async Task UploadToQuarantineAsync_WhenStorageSucceeds_ReturnsStoragePath()
    {
        var ownerId = Guid.NewGuid();
        var mediaFileId = Guid.NewGuid();
        var path = $"quarantine/listing/{ownerId}/{mediaFileId}.jpg";
        _storageService
            .Setup(s => s.UploadToQuarantineAsync(
                It.IsAny<Stream>(),
                "photo.jpg",
                "listing",
                ownerId,
                mediaFileId,
                "image/jpeg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(path));

        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);
        var result = await _service.UploadToQuarantineAsync(
            stream,
            "photo.jpg",
            OwnerType.Listing,
            ownerId,
            mediaFileId,
            AllowedContentTypes.Jpeg,
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(path, result.Value!.Value);
    }

    [TestMethod]
    public async Task UploadToQuarantineAsync_WhenStorageFails_PropagatesError()
    {
        var error = SharedFailures.SystemFailed;
        _storageService
            .Setup(s => s.UploadToQuarantineAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure(error));

        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);
        var result = await _service.UploadToQuarantineAsync(
            stream,
            "photo.jpg",
            OwnerType.Listing,
            Guid.NewGuid(),
            Guid.NewGuid(),
            AllowedContentTypes.Jpeg,
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error.Code.Value, result.Error!.Code.Value);
    }
}
