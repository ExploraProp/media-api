using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Services;

namespace Media.Unit.Infrastructure.Services;

[TestClass]
public class MediaUploadFileServiceTests
{
    private readonly Mock<IContentTypeDetectionService> _contentTypeDetectionService = new();
    private MediaUploadFileService _service = null!;

    [TestInitialize]
    public void Setup() =>
        _service = new MediaUploadFileService(_contentTypeDetectionService.Object);

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
}
