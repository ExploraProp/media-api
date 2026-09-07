using Media.Domain.Enums.Properties;
using Media.Infrastructure.Services;

namespace Media.Unit.Infrastructure.Services;

[TestClass]
public class ContentTypeDetectionServiceTests
{
    private readonly ContentTypeDetectionService _service = new();

    [TestMethod]
    public async Task DetectAsync_WithJpegHeader_ShouldReturnJpegAndResetStream()
    {
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10]);

        var result = await _service.DetectAsync(stream, CancellationToken.None);

        Assert.AreEqual(AllowedContentTypes.Jpeg, result);
        Assert.AreEqual(0, stream.Position);
    }

    [TestMethod]
    public async Task DetectAsync_WithWebPHeader_ShouldReturnWebP()
    {
        var header = new byte[]
        {
            0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50,
        };
        await using var stream = new MemoryStream(header);

        var result = await _service.DetectAsync(stream, CancellationToken.None);

        Assert.AreEqual(AllowedContentTypes.WebP, result);
    }

    [TestMethod]
    public async Task DetectAsync_WithUnknownHeader_ShouldReturnNull()
    {
        await using var stream = new MemoryStream([0x4D, 0x5A]);

        var result = await _service.DetectAsync(stream, CancellationToken.None);

        Assert.IsNull(result);
    }
}
