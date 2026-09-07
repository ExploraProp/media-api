using Media.Domain.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Services;

namespace Media.Unit.Infrastructure.Services;

[TestClass]
public class MediaEventChannelTests
{
    private readonly MediaEventChannel _channel = new();

    [TestMethod]
    public async Task Publish_WhenWaiterRegistered_ShouldCompleteWait()
    {
        var mediaFileId = MediaFileId.New();
        var mediaEvent = new MediaFileEvent("fileRejected", new { reason = "infected" });

        var waitTask = _channel.WaitAsync(mediaFileId, CancellationToken.None);
        _channel.Publish(mediaFileId, mediaEvent);

        var result = await waitTask;

        Assert.AreEqual(mediaEvent, result);
    }

    [TestMethod]
    public void Publish_WhenNoWaiterRegistered_ShouldNotThrow()
    {
        var mediaFileId = MediaFileId.New();
        var mediaEvent = new MediaFileEvent("fileApproved", new { });

        _channel.Publish(mediaFileId, mediaEvent);
    }

    [TestMethod]
    public async Task WaitAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        var mediaFileId = MediaFileId.New();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(
            () => _channel.WaitAsync(mediaFileId, cts.Token));
    }

    [TestMethod]
    public async Task WaitAsync_WhenCancelledAfterRegistration_ShouldRemoveWaiter()
    {
        var mediaFileId = MediaFileId.New();
        using var cts = new CancellationTokenSource();

        var waitTask = _channel.WaitAsync(mediaFileId, cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => waitTask);

        var mediaEvent = new MediaFileEvent("fileApproved", new { });
        _channel.Publish(mediaFileId, mediaEvent);

        var secondWait = _channel.WaitAsync(mediaFileId, CancellationToken.None);
        _channel.Publish(mediaFileId, mediaEvent);

        Assert.AreEqual(mediaEvent, await secondWait);
    }
}
