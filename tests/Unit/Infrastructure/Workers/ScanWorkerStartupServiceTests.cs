using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Workers;
using Media.Unit.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Kernel.Results;
using System.Threading.Channels;

namespace Media.Unit.Infrastructure.Workers;

[TestClass]
public class ScanWorkerStartupServiceTests
{
    private readonly Mock<IMediaUploadRepo> _mediaUploadRepo = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IServiceScope> _scope = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private Channel<MediaFileId> _channel = null!;
    private ScanWorkerStartupService _service = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());

    [TestInitialize]
    public void Setup()
    {
        _channel = Channel.CreateUnbounded<MediaFileId>(new UnboundedChannelOptions { SingleReader = true });

        _scopeFactory.Setup(f => f.CreateScope()).Returns(_scope.Object);
        _scope.Setup(s => s.ServiceProvider).Returns(_serviceProvider.Object);
        _serviceProvider.Setup(p => p.GetService(typeof(IMediaUploadRepo))).Returns(_mediaUploadRepo.Object);

        _service = new ScanWorkerStartupService(
            _scopeFactory.Object,
            _channel,
            NullLogger<ScanWorkerStartupService>.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope.Object.Dispose();
    }

    private static MediaUpload CreateUpload(MediaStatus status)
    {
        var upload = MediaTestFactory.CreateUpload(ownerId: OwnerId);
        if (status == MediaStatus.Scanning)
            upload.BeginScan();
        return upload;
    }

    [TestMethod]
    public async Task StartAsync_WhenPendingAndScanningFilesExist_ShouldEnqueueAllIds()
    {
        var pending = CreateUpload(MediaStatus.Pending);
        var scanning = CreateUpload(MediaStatus.Scanning);

        _mediaUploadRepo
            .Setup(r => r.GetByStatusesAsync(
                It.Is<IReadOnlyList<MediaStatus>>(l =>
                    l.Contains(MediaStatus.Pending) && l.Contains(MediaStatus.Scanning)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaUpload>>.Success(
                new List<MediaUpload> { pending, scanning }));

        await _service.StartAsync(CancellationToken.None);

        Assert.IsTrue(_channel.Reader.TryRead(out var first));
        Assert.IsTrue(_channel.Reader.TryRead(out var second));
        var enqueued = new[] { first!, second! };
        CollectionAssert.Contains(enqueued, pending.Id);
        CollectionAssert.Contains(enqueued, scanning.Id);
        Assert.IsFalse(_channel.Reader.TryRead(out _), "no extra items should be enqueued");
    }

    [TestMethod]
    public async Task StartAsync_WhenNoFilesExist_ShouldNotEnqueueAnything()
    {
        _mediaUploadRepo
            .Setup(r => r.GetByStatusesAsync(It.IsAny<IReadOnlyList<MediaStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaUpload>>.Success(
                new List<MediaUpload>()));

        await _service.StartAsync(CancellationToken.None);

        Assert.IsFalse(_channel.Reader.TryRead(out _), "channel should be empty when no files found");
    }

    [TestMethod]
    public async Task StartAsync_WhenRepoReturnsFailure_ShouldNotThrowAndNotEnqueue()
    {
        _mediaUploadRepo
            .Setup(r => r.GetByStatusesAsync(It.IsAny<IReadOnlyList<MediaStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaUpload>>.Failure(SharedFailures.SystemFailed));

        await _service.StartAsync(CancellationToken.None);

        Assert.IsFalse(_channel.Reader.TryRead(out _), "channel should be empty on repo failure");
    }

    [TestMethod]
    public async Task StartAsync_QueriesForBothPendingAndScanningStatuses()
    {
        _mediaUploadRepo
            .Setup(r => r.GetByStatusesAsync(It.IsAny<IReadOnlyList<MediaStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaUpload>>.Success(new List<MediaUpload>()));

        await _service.StartAsync(CancellationToken.None);

        _mediaUploadRepo.Verify(r => r.GetByStatusesAsync(
            It.Is<IReadOnlyList<MediaStatus>>(l =>
                l.Contains(MediaStatus.Pending) &&
                l.Contains(MediaStatus.Scanning) &&
                l.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task StopAsync_ShouldCompleteWithoutError()
    {
        await _service.StopAsync(CancellationToken.None);
    }
}
