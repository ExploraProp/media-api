using Media.Application.Commands;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Unit.TestSupport;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class RejectMediaUseCaseTests
{
    private readonly Mock<IMediaUploadRepo> _mediaUploadRepo = new();
    private readonly Mock<IStorageService> _storageService = new();
    private RejectMediaUseCase _useCase = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());

    [TestInitialize]
    public void Setup()
    {
        _useCase = new RejectMediaUseCase(
            _mediaUploadRepo.Object,
            _storageService.Object);

        _storageService
            .Setup(s => s.DeleteFileAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mediaUploadRepo
            .Setup(r => r.UpdateAsync(It.IsAny<MediaUpload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
    }

    private static MediaUpload CreateScanningUpload()
    {
        var upload = MediaTestFactory.CreateUpload(ownerId: OwnerId);
        upload.BeginScan();
        return upload;
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ShouldPropagateNotFound()
    {
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Failure(SharedFailures.NotFound(nameof(MediaUpload))));

        var result = await _useCase.Handle(
            new RejectMediaCommand(MediaFileId.New(), ScanRejectionReason.Infected),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaUpload)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenReasonIsInfected_ShouldSetStatusToRejected()
    {
        var upload = CreateScanningUpload();
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));

        await _useCase.Handle(
            new RejectMediaCommand(upload.Id, ScanRejectionReason.Infected),
            CancellationToken.None);

        Assert.AreEqual(MediaStatus.Rejected, upload.Status);
    }

    [TestMethod]
    public async Task Handle_WhenReasonIsScanFailed_ShouldSetStatusToFailed()
    {
        var upload = CreateScanningUpload();
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));

        await _useCase.Handle(
            new RejectMediaCommand(upload.Id, ScanRejectionReason.ScanFailed),
            CancellationToken.None);

        Assert.AreEqual(MediaStatus.Failed, upload.Status);
    }

    [TestMethod]
    public async Task Handle_WithAnyReason_ShouldDeleteQuarantineFile()
    {
        var upload = CreateScanningUpload();
        var expectedPath = upload.QuarantinePath.Value;
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));

        await _useCase.Handle(
            new RejectMediaCommand(upload.Id, ScanRejectionReason.Infected),
            CancellationToken.None);

        _storageService.Verify(s => s.DeleteFileAsync(expectedPath), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithAnyReason_ShouldCallUpdateAsync()
    {
        var upload = CreateScanningUpload();
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));

        var result = await _useCase.Handle(
            new RejectMediaCommand(upload.Id, ScanRejectionReason.ScanFailed),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        _mediaUploadRepo.Verify(r => r.UpdateAsync(upload, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithAnyReason_ShouldRaiseFileRejectedDomainEvent()
    {
        var upload = CreateScanningUpload();
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));

        await _useCase.Handle(
            new RejectMediaCommand(upload.Id, ScanRejectionReason.Infected),
            CancellationToken.None);

        Assert.AreEqual(1, upload.DomainEvents.Count);
        Assert.IsInstanceOfType<FileRejected>(upload.DomainEvents.First());
    }

    [TestMethod]
    public async Task Handle_WhenUpdateFails_ShouldPropagateError()
    {
        var upload = CreateScanningUpload();
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));
        _mediaUploadRepo
            .Setup(r => r.UpdateAsync(It.IsAny<MediaUpload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(SharedFailures.SystemFailed));

        var result = await _useCase.Handle(
            new RejectMediaCommand(upload.Id, ScanRejectionReason.Infected),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
    }
}
