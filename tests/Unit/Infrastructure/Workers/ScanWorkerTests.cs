using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using System.Threading.Channels;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Infrastructure.Workers;

[TestClass]
public class ScanWorkerTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IScanService> _scanService = new();
    private readonly Mock<IMediaScanOrchestrator> _scanOrchestrator = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<IDeadLetterStore> _deadLetterStore = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IServiceScope> _scope = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private Channel<MediaFileId> _channel = null!;
    private ScanWorker _worker = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());

    [TestInitialize]
    public void Setup()
    {
        _channel = Channel.CreateUnbounded<MediaFileId>(new UnboundedChannelOptions { SingleReader = true });

        _scopeFactory.Setup(f => f.CreateScope()).Returns(_scope.Object);
        _scope.Setup(s => s.ServiceProvider).Returns(_serviceProvider.Object);
        _serviceProvider.Setup(p => p.GetService(typeof(IMediaRepo))).Returns(_mediaRepo.Object);
        _serviceProvider.Setup(p => p.GetService(typeof(IScanService))).Returns(_scanService.Object);
        _serviceProvider.Setup(p => p.GetService(typeof(IMediaScanOrchestrator))).Returns(_scanOrchestrator.Object);
        _serviceProvider.Setup(p => p.GetService(typeof(IStorageService))).Returns(_storageService.Object);
        _serviceProvider.Setup(p => p.GetService(typeof(IDeadLetterStore))).Returns(_deadLetterStore.Object);

        _scanOrchestrator
            .Setup(o => o.ApproveAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _scanOrchestrator
            .Setup(o => o.RejectAsync(It.IsAny<MediaFileId>(), It.IsAny<ScanRejectionReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _deadLetterStore
            .Setup(d => d.RecordAsync(It.IsAny<MediaFileId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _worker = new ScanWorker(
            _scopeFactory.Object,
            _channel,
            ScanWorkerImmediateTimeProvider.Instance,
            NullLogger<ScanWorker>.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope.Object.Dispose();
    }

    private MediaFile CreatePendingMediaFile(MediaFileId? id = null)
    {
        var fileId = id ?? MediaFileId.New();
        return MediaFile.Create(
            fileId,
            OwnerId,
            OwnerType.Listing,
            UploadMediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase($"quarantine/listing/{OwnerId.Value}/{fileId.Value}.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;
    }

    private void SetupMediaRepo(MediaFile mediaFile)
    {
        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));
        _mediaRepo
            .Setup(r => r.UpdateAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
    }

    private void SetupCleanScan()
    {
        _storageService
            .Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Stream>.Success((Stream)new MemoryStream(new byte[10])));
        _scanService
            .Setup(s => s.ScanAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ScanResult.Clean);
    }

    private void SetupInfectedScan()
    {
        _storageService
            .Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Stream>.Success((Stream)new MemoryStream(new byte[10])));
        _scanService
            .Setup(s => s.ScanAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ScanResult.Infected);
    }

    private async Task RunWorkerAndWaitAsync(MediaFileId id, int expectedOrchestratorCalls = 1)
    {
        await _worker.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(id);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var callCount = _scanOrchestrator.Invocations.Count;
            if (callCount >= expectedOrchestratorCalls) break;
            await Task.Delay(20);
        }

        await _worker.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task StartAsync_WhenFileIsClean_ShouldCallApproveAsync()
    {
        var mediaFile = CreatePendingMediaFile();
        SetupMediaRepo(mediaFile);
        SetupCleanScan();

        await RunWorkerAndWaitAsync(mediaFile.Id);

        _scanOrchestrator.Verify(
            o => o.ApproveAsync(mediaFile.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_WhenFileIsInfected_ShouldCallRejectWithInfectedReason()
    {
        var mediaFile = CreatePendingMediaFile();
        SetupMediaRepo(mediaFile);
        SetupInfectedScan();

        await RunWorkerAndWaitAsync(mediaFile.Id);

        _scanOrchestrator.Verify(
            o => o.RejectAsync(mediaFile.Id, ScanRejectionReason.Infected, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_WhenMediaFileNotFound_ShouldNotCallOrchestrator()
    {
        var id = MediaFileId.New();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));

        await _worker.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(id);
        await Task.Delay(200);
        await _worker.StopAsync(CancellationToken.None);

        _scanOrchestrator.Verify(
            o => o.ApproveAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _scanOrchestrator.Verify(
            o => o.RejectAsync(It.IsAny<MediaFileId>(), It.IsAny<ScanRejectionReason>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task StartAsync_WhenAllScanAttemptsThrow_ShouldCallRejectWithScanFailedReason()
    {
        var mediaFile = CreatePendingMediaFile();
        SetupMediaRepo(mediaFile);

        _storageService
            .Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused"));

        var deadline = DateTime.UtcNow.AddSeconds(5);
        await _worker.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(mediaFile.Id);

        while (DateTime.UtcNow < deadline)
        {
            if (_scanOrchestrator.Invocations.Count >= 1) break;
            await Task.Delay(20);
        }

        await _worker.StopAsync(CancellationToken.None);

        _scanOrchestrator.Verify(
            o => o.RejectAsync(mediaFile.Id, ScanRejectionReason.ScanFailed, It.IsAny<CancellationToken>()),
            Times.Once);

        _deadLetterStore.Verify(
            d => d.RecordAsync(mediaFile.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task StartAsync_WhenFileIsNotPending_ShouldNotCallOrchestrator()
    {
        var id = MediaFileId.New();
        var mediaFile = MediaFile.Create(
            id,
            OwnerId,
            OwnerType.Listing,
            UploadMediaType.Photo,
            MediaStatus.Scanning,
            StoragePath.FromDatabase($"quarantine/listing/{OwnerId.Value}/{id.Value}.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

        SetupMediaRepo(mediaFile);

        await _worker.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(id);
        await Task.Delay(200);
        await _worker.StopAsync(CancellationToken.None);

        _scanOrchestrator.Verify(
            o => o.ApproveAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _scanOrchestrator.Verify(
            o => o.RejectAsync(It.IsAny<MediaFileId>(), It.IsAny<ScanRejectionReason>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

internal sealed class ScanWorkerImmediateTimeProvider : TimeProvider
{
    public static readonly ScanWorkerImmediateTimeProvider Instance = new();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        => base.CreateTimer(callback, state, TimeSpan.Zero, TimeSpan.Zero);
}
