using Media.Application.Services;
using Media.Domain.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;

namespace Media.Unit.Infrastructure.Services;

[TestClass]
public class MediaEventWaitServiceTests
{
    private readonly Mock<IMediaEventChannel> _mediaEventChannel = new();
    private MediaEventWaitService _service = null!;

    [TestInitialize]
    public void Setup() => _service = new MediaEventWaitService(_mediaEventChannel.Object);

    [TestMethod]
    public async Task WaitForTerminalEventAsync_WhenEventPublished_ShouldReturnEvent()
    {
        var mediaFileId = MediaFileId.New();
        var mediaEvent = new MediaFileEvent("fileApproved", new { mediaFileId = mediaFileId.Value });

        _mediaEventChannel
            .Setup(c => c.WaitAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaEvent);

        var result = await _service.WaitForTerminalEventAsync(mediaFileId, CancellationToken.None);

        Assert.IsFalse(result.IsTimeout);
        Assert.AreEqual(mediaEvent, result.Event);
    }

    [TestMethod]
    public async Task WaitForTerminalEventAsync_WhenWaitTimesOut_ShouldReturnTimeout()
    {
        var mediaFileId = MediaFileId.New();

        _mediaEventChannel
            .Setup(c => c.WaitAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await _service.WaitForTerminalEventAsync(mediaFileId, CancellationToken.None);

        Assert.IsTrue(result.IsTimeout);
        Assert.IsNull(result.Event);
    }
}
