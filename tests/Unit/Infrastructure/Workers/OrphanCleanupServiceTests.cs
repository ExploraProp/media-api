using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Infrastructure.Workers;

[TestClass]
public class OrphanCleanupServiceTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IServiceScope> _scope = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private OrphanCleanupService _service = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());

    [TestInitialize]
    public void Setup()
    {
        _scopeFactory.Setup(f => f.CreateScope()).Returns(_scope.Object);
        _scope.Setup(s => s.ServiceProvider).Returns(_serviceProvider.Object);
        _serviceProvider.Setup(p => p.GetService(typeof(IMediaRepo))).Returns(_mediaRepo.Object);
        _serviceProvider.Setup(p => p.GetService(typeof(IStorageService))).Returns(_storageService.Object);

        _storageService.Setup(s => s.DeleteFileAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _storageService.Setup(s => s.DeleteFileByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _mediaRepo
            .Setup(r => r.DeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _service = BuildService();
    }

    [TestCleanup]
    public void Cleanup() => _scope.Object.Dispose();

    private OrphanCleanupService BuildService(TimeSpan? interval = null, int ttlHours = 24) =>
        new(_scopeFactory.Object, NullLogger<OrphanCleanupService>.Instance,
            interval ?? TimeSpan.FromMilliseconds(50), ttlHours);

    private static MediaFile CreateMediaFile(MediaStatus status, DateTime? uploadedAt = null)
    {
        var id = MediaFileId.New();
        var mediaFile = MediaFile.Create(
            id,
            OwnerId,
            OwnerType.Listing,
            UploadMediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase($"quarantine/listing/{OwnerId.Value}/{id.Value}.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

        return status switch
        {
            MediaStatus.Scanning => BeginScan(mediaFile),
            MediaStatus.Failed => MarkFailed(mediaFile),
            MediaStatus.Approved => Approve(mediaFile),
            MediaStatus.Rejected => Reject(mediaFile),
            _ => mediaFile,
        };
    }

    private static MediaFile BeginScan(MediaFile f) { f.BeginScan(); return f; }
    private static MediaFile MarkFailed(MediaFile f)
    {
        f.BeginScan();
        f.MarkFailed("scan error");
        f.ClearDomainEvents();
        return f;
    }
    private static MediaFile Approve(MediaFile f)
    {
        f.BeginScan();
        f.Approve(null, AccessLevel.Private, []);
        f.ClearDomainEvents();
        return f;
    }
    private static MediaFile Reject(MediaFile f)
    {
        f.BeginScan();
        f.Reject("infected");
        f.ClearDomainEvents();
        return f;
    }

    private void SetupOrphans(IReadOnlyList<MediaFile> orphans) =>
        _mediaRepo
            .Setup(r => r.GetOrphansAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaFile>>.Success(orphans));

    // ── happy path ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task CleanupAsync_WithPendingOrphan_ShouldDeleteQuarantineBlobAndDbRecord()
    {
        var orphan = CreateMediaFile(MediaStatus.Pending);
        SetupOrphans([orphan]);

        await _service.CleanupAsync(CancellationToken.None);

        _storageService.Verify(s => s.DeleteFileAsync(orphan.QuarantinePath.Value), Times.Once);
        _mediaRepo.Verify(r => r.DeleteAsync(orphan.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CleanupAsync_WithFailedOrphan_ShouldDeleteQuarantineBlobAndDbRecord()
    {
        var orphan = CreateMediaFile(MediaStatus.Failed);
        SetupOrphans([orphan]);

        await _service.CleanupAsync(CancellationToken.None);

        _storageService.Verify(s => s.DeleteFileAsync(orphan.QuarantinePath.Value), Times.Once);
        _mediaRepo.Verify(r => r.DeleteAsync(orphan.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CleanupAsync_WithPendingOrphan_ShouldRaiseFileDeletedEvent()
    {
        var orphan = CreateMediaFile(MediaStatus.Pending);
        SetupOrphans([orphan]);

        await _service.CleanupAsync(CancellationToken.None);

        Assert.AreEqual(1, orphan.DomainEvents.Count,
            "FileDeleted must be queued on the aggregate for the EF interceptor to dispatch");
        Assert.IsInstanceOfType<FileDeleted>(orphan.DomainEvents.Single());
    }

    [TestMethod]
    public async Task CleanupAsync_WithMultipleOrphans_ShouldPublishFileDeletedForEach()
    {
        var orphan1 = CreateMediaFile(MediaStatus.Pending);
        var orphan2 = CreateMediaFile(MediaStatus.Failed);
        SetupOrphans([orphan1, orphan2]);

        await _service.CleanupAsync(CancellationToken.None);

        Assert.IsInstanceOfType<FileDeleted>(orphan1.DomainEvents.Single());
        Assert.IsInstanceOfType<FileDeleted>(orphan2.DomainEvents.Single());
        _mediaRepo.Verify(r => r.DeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task CleanupAsync_WhenNoOrphans_ShouldNotCallDeleteAsync()
    {
        SetupOrphans([]);

        await _service.CleanupAsync(CancellationToken.None);

        _mediaRepo.Verify(r => r.DeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageService.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    // ── query contract ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task CleanupAsync_ShouldCallGetOrphansWithMaxCount100()
    {
        SetupOrphans([]);

        await _service.CleanupAsync(CancellationToken.None);

        _mediaRepo.Verify(r => r.GetOrphansAsync(
            It.IsAny<DateTime>(), 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CleanupAsync_ShouldPassPendingBeforeCutoffDerivedFromTtl()
    {
        var ttlHours = 24;
        var svc = BuildService(ttlHours: ttlHours);
        SetupOrphans([]);

        var before = DateTime.UtcNow.AddHours(-ttlHours);
        await svc.CleanupAsync(CancellationToken.None);
        var after = DateTime.UtcNow.AddHours(-ttlHours);

        _mediaRepo.Verify(r => r.GetOrphansAsync(
            It.Is<DateTime>(dt => dt >= before && dt <= after),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── fault tolerance ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task CleanupAsync_WhenQuarantineBlobMissing_ShouldContinueAndDeleteDbRecord()
    {
        var orphan = CreateMediaFile(MediaStatus.Pending);
        SetupOrphans([orphan]);
        _storageService
            .Setup(s => s.DeleteFileAsync(orphan.QuarantinePath.Value))
            .ThrowsAsync(new InvalidOperationException("blob not found"));

        await _service.CleanupAsync(CancellationToken.None);

        _mediaRepo.Verify(r => r.DeleteAsync(orphan.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CleanupAsync_WhenFirstBlobFailsForOneOrphan_ShouldStillCleanSecondOrphan()
    {
        var orphan1 = CreateMediaFile(MediaStatus.Pending);
        var orphan2 = CreateMediaFile(MediaStatus.Pending);
        SetupOrphans([orphan1, orphan2]);
        _storageService
            .Setup(s => s.DeleteFileAsync(orphan1.QuarantinePath.Value))
            .ThrowsAsync(new InvalidOperationException("storage error"));

        await _service.CleanupAsync(CancellationToken.None);

        _mediaRepo.Verify(r => r.DeleteAsync(orphan1.Id, It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepo.Verify(r => r.DeleteAsync(orphan2.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CleanupAsync_WhenRepoGetOrphansFails_ShouldNotCallDeleteAsync()
    {
        _mediaRepo
            .Setup(r => r.GetOrphansAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<MediaFile>>.Failure(SharedFailures.SystemFailed));

        await _service.CleanupAsync(CancellationToken.None);

        _mediaRepo.Verify(r => r.DeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CleanupAsync_WhenDbDeleteFails_ShouldContinueToNextOrphan()
    {
        var orphan1 = CreateMediaFile(MediaStatus.Pending);
        var orphan2 = CreateMediaFile(MediaStatus.Pending);
        SetupOrphans([orphan1, orphan2]);
        _mediaRepo
            .Setup(r => r.DeleteAsync(orphan1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(SharedFailures.SystemFailed));

        await _service.CleanupAsync(CancellationToken.None);

        _mediaRepo.Verify(r => r.DeleteAsync(orphan2.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── timer-driven lifecycle ───────────────────────────────────────────────

    [TestMethod]
    public async Task StartAsync_ShouldTriggerCleanupAfterInterval()
    {
        SetupOrphans([]);
        await _service.StartAsync(CancellationToken.None);

        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            if (_mediaRepo.Invocations.Any(i => i.Method.Name == nameof(IMediaRepo.GetOrphansAsync))) break;
            await Task.Delay(20);
        }

        await _service.StopAsync(CancellationToken.None);

        _mediaRepo.Verify(r => r.GetOrphansAsync(
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task StopAsync_ShouldCompleteWithoutError()
    {
        await _service.StopAsync(CancellationToken.None);
    }
}
