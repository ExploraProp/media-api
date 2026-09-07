using Media.Application.Commands;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class RejectMediaUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IStorageService> _storageService = new();
    private RejectMediaUseCase _useCase = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());

    [TestInitialize]
    public void Setup()
    {
        _useCase = new RejectMediaUseCase(
            _mediaRepo.Object,
            _storageService.Object);

        _storageService
            .Setup(s => s.DeleteFileAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mediaRepo
            .Setup(r => r.UpdateAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
    }

    private static MediaFile CreateScanningMediaFile()
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
        mediaFile.BeginScan();
        return mediaFile;
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ShouldPropagateNotFound()
    {
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));

        var result = await _useCase.Handle(
            new RejectMediaCommand(MediaFileId.New(), ScanRejectionReason.Infected),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaFile)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenReasonIsInfected_ShouldSetStatusToRejected()
    {
        var mediaFile = CreateScanningMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        await _useCase.Handle(
            new RejectMediaCommand(mediaFile.Id, ScanRejectionReason.Infected),
            CancellationToken.None);

        Assert.AreEqual(MediaStatus.Rejected, mediaFile.Status);
    }

    [TestMethod]
    public async Task Handle_WhenReasonIsScanFailed_ShouldSetStatusToFailed()
    {
        var mediaFile = CreateScanningMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        await _useCase.Handle(
            new RejectMediaCommand(mediaFile.Id, ScanRejectionReason.ScanFailed),
            CancellationToken.None);

        Assert.AreEqual(MediaStatus.Failed, mediaFile.Status);
    }

    [TestMethod]
    public async Task Handle_WithAnyReason_ShouldDeleteQuarantineFile()
    {
        var mediaFile = CreateScanningMediaFile();
        var expectedPath = mediaFile.QuarantinePath.Value;
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        await _useCase.Handle(
            new RejectMediaCommand(mediaFile.Id, ScanRejectionReason.Infected),
            CancellationToken.None);

        _storageService.Verify(s => s.DeleteFileAsync(expectedPath), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithAnyReason_ShouldCallUpdateAsync()
    {
        var mediaFile = CreateScanningMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(
            new RejectMediaCommand(mediaFile.Id, ScanRejectionReason.ScanFailed),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        _mediaRepo.Verify(r => r.UpdateAsync(mediaFile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithAnyReason_ShouldRaiseFileRejectedDomainEvent()
    {
        var mediaFile = CreateScanningMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        await _useCase.Handle(
            new RejectMediaCommand(mediaFile.Id, ScanRejectionReason.Infected),
            CancellationToken.None);

        Assert.AreEqual(1, mediaFile.DomainEvents.Count);
        Assert.IsInstanceOfType<FileRejected>(mediaFile.DomainEvents.First());
    }

    [TestMethod]
    public async Task Handle_WhenUpdateFails_ShouldPropagateError()
    {
        var mediaFile = CreateScanningMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));
        _mediaRepo
            .Setup(r => r.UpdateAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(SharedFailures.SystemFailed));

        var result = await _useCase.Handle(
            new RejectMediaCommand(mediaFile.Id, ScanRejectionReason.Infected),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
    }
}
