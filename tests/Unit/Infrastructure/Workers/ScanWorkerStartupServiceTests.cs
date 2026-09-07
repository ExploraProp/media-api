using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Kernel.Results;
using System.Threading.Channels;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Infrastructure.Workers;

[TestClass]
public class ScanWorkerStartupServiceTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
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
        _serviceProvider.Setup(p => p.GetService(typeof(IMediaRepo))).Returns(_mediaRepo.Object);

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

    private static MediaFile CreateMediaFile(MediaStatus status)
    {
        var id = MediaFileId.New();
        return MediaFile.Create(
            id,
            OwnerId,
            OwnerType.Listing,
            UploadMediaType.Photo,
            status,
            StoragePath.FromDatabase($"quarantine/listing/{OwnerId.Value}/{id.Value}.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;
    }

    [TestMethod]
    public async Task StartAsync_WhenPendingAndScanningFilesExist_ShouldEnqueueAllIds()
    {
        var pending = CreateMediaFile(MediaStatus.Pending);
        var scanning = CreateMediaFile(MediaStatus.Scanning);

        _mediaRepo
            .Setup(r => r.GetByStatusesAsync(
                It.Is<IReadOnlyList<MediaStatus>>(l =>
                    l.Contains(MediaStatus.Pending) && l.Contains(MediaStatus.Scanning)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaFile>>.Success(
                new List<MediaFile> { pending, scanning }));

        await _service.StartAsync(CancellationToken.None);

        // Both IDs should be in the channel
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
        _mediaRepo
            .Setup(r => r.GetByStatusesAsync(It.IsAny<IReadOnlyList<MediaStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaFile>>.Success(
                new List<MediaFile>()));

        await _service.StartAsync(CancellationToken.None);

        Assert.IsFalse(_channel.Reader.TryRead(out _), "channel should be empty when no files found");
    }

    [TestMethod]
    public async Task StartAsync_WhenRepoReturnsFailure_ShouldNotThrowAndNotEnqueue()
    {
        _mediaRepo
            .Setup(r => r.GetByStatusesAsync(It.IsAny<IReadOnlyList<MediaStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaFile>>.Failure(SharedFailures.SystemFailed));

        await _service.StartAsync(CancellationToken.None);

        Assert.IsFalse(_channel.Reader.TryRead(out _), "channel should be empty on repo failure");
    }

    [TestMethod]
    public async Task StartAsync_QueriesForBothPendingAndScanningStatuses()
    {
        _mediaRepo
            .Setup(r => r.GetByStatusesAsync(It.IsAny<IReadOnlyList<MediaStatus>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaFile>>.Success(new List<MediaFile>()));

        await _service.StartAsync(CancellationToken.None);

        _mediaRepo.Verify(r => r.GetByStatusesAsync(
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
