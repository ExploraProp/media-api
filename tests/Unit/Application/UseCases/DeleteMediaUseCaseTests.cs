using Media.Application.Commands;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
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
public class DeleteMediaUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<ILogger<DeleteMediaUseCase>> _logger = new();
    private DeleteMediaUseCase _useCase = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());
    private static readonly UploadedByClientId UploaderId = UploadedByClientId.New();
    private static readonly UploadedByClientId OtherUserId = UploadedByClientId.New();

    [TestInitialize]
    public void Setup()
    {
        _useCase = new DeleteMediaUseCase(_mediaRepo.Object, _storageService.Object, _logger.Object);

        _storageService.Setup(s => s.DeleteFileAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _storageService.Setup(s => s.DeleteFileByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _mediaRepo
            .Setup(r => r.DeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
    }

    private static MediaFile CreatePendingMediaFile(UploadedByClientId? uploadedBy = null)
    {
        var id = MediaFileId.New();
        return MediaFile.Create(
            id,
            OwnerId,
            OwnerType.Listing,
            UploadMediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase($"quarantine/listing/{OwnerId.Value}/{id.Value}.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            uploadedBy ?? UploaderId).Value!;
    }

    private static MediaFile CreateScanningMediaFile(UploadedByClientId? uploadedBy = null)
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
            uploadedBy ?? UploaderId).Value!;
        mediaFile.BeginScan();
        return mediaFile;
    }

    private static MediaFile CreateApprovedMediaFileWithVariants(UploadedByClientId? uploadedBy = null)
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
            uploadedBy ?? UploaderId).Value!;

        var variants = new List<PromotedVariantInput>
        {
            new(VariantType.Original,
                VariantUrl.FromStorage("https://firebase.storage/listing/original.jpg"),
                null, null, MediaFileSize.FromDatabase(5000)),
            new(VariantType.Thumbnail,
                VariantUrl.FromStorage("https://firebase.storage/listing/thumbnail.webp"),
                VariantDimension.From(200), VariantDimension.From(200), MediaFileSize.FromDatabase(1000)),
        };

        mediaFile.BeginScan();
        mediaFile.Approve("https://firebase.storage/listing/original.jpg", AccessLevel.Public, variants);
        return mediaFile;
    }

    private static MediaFile CreateApprovedMediaFileWithPrivateVariant(UploadedByClientId? uploadedBy = null)
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
            uploadedBy ?? UploaderId).Value!;

        var variants = new List<PromotedVariantInput>
        {
            new(VariantType.Original,
                VariantUrl.FromStorage("listing/owner/original.jpg"),
                null, null, MediaFileSize.FromDatabase(5000)),
        };

        mediaFile.BeginScan();
        mediaFile.Approve(null, AccessLevel.Private, variants);
        return mediaFile;
    }

    private DeleteMediaCommand AsOwner(MediaFileId id) =>
        new(id, UploaderId, IsAdmin: false);

    private static DeleteMediaCommand AsAdmin(MediaFileId id) =>
        new(id, OtherUserId, IsAdmin: true);

    // ── 404 ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ShouldPropagateNotFound()
    {
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));

        var result = await _useCase.Handle(
            AsAdmin(MediaFileId.New()), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaFile)).Code.Value, result.Error!.Code.Value);
    }

    // ── 403 ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_WhenRequestingUserIsNeitherUploaderNorAdmin_ShouldReturnForbidden()
    {
        var mediaFile = CreatePendingMediaFile(uploadedBy: UploaderId);
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var command = new DeleteMediaCommand(mediaFile.Id, OtherUserId, IsAdmin: false);
        var result = await _useCase.Handle(command, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(HttpStatusCode.Forbidden, result.Error!.StatusCode);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenRequestingUserIsUploader_ShouldSucceed()
    {
        var mediaFile = CreatePendingMediaFile(uploadedBy: UploaderId);
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task Handle_WhenAdminDeletesAnotherUsersFile_ShouldSucceed()
    {
        var mediaFile = CreatePendingMediaFile(uploadedBy: UploaderId);
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(AsAdmin(mediaFile.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
    }

    // ── 409 ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_WhenFileIsScanning_ShouldReturnOngoingScanning()
    {
        var mediaFile = CreateScanningMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(HttpStatusCode.Conflict, result.Error!.StatusCode);
        Assert.AreEqual(MediaFileError.OngoingScanning.Code.Value, result.Error!.Code.Value);
    }

    // ── Storage deletion ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_WithApprovedFile_ShouldDeleteVariantsByUrl()
    {
        var mediaFile = CreateApprovedMediaFileWithVariants();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        _storageService.Verify(
            s => s.DeleteFileByUrlAsync("https://firebase.storage/listing/original.jpg"),
            Times.Once);
        _storageService.Verify(
            s => s.DeleteFileByUrlAsync("https://firebase.storage/listing/thumbnail.webp"),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithApprovedFile_ShouldNotDeleteQuarantineFile()
    {
        var mediaFile = CreateApprovedMediaFileWithVariants();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        _storageService.Verify(
            s => s.DeleteFileAsync(mediaFile.QuarantinePath.Value),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_WithPendingFile_ShouldDeleteQuarantineFile()
    {
        var mediaFile = CreatePendingMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        _storageService.Verify(s => s.DeleteFileAsync(mediaFile.QuarantinePath.Value), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithPrivateVariantUrl_ShouldDeleteFileByPath()
    {
        var mediaFile = CreateApprovedMediaFileWithPrivateVariant();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        _storageService.Verify(
            s => s.DeleteFileAsync("listing/owner/original.jpg"),
            Times.Once);
        _storageService.Verify(
            s => s.DeleteFileByUrlAsync(It.IsAny<string>()),
            Times.Never);
    }

    // ── DB delete & event ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handle_WithAnyFile_ShouldCallDeleteAsync()
    {
        var mediaFile = CreatePendingMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        _mediaRepo.Verify(
            r => r.DeleteAsync(mediaFile.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_AfterSuccessfulDeletion_FileDeletedEventIsRaised()
    {
        var mediaFile = CreatePendingMediaFile();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        Assert.AreEqual(1, mediaFile.DomainEvents.Count,
            "exactly one FileDeleted event must be queued on the aggregate for the EF interceptor to dispatch");
        Assert.IsInstanceOfType<FileDeleted>(mediaFile.DomainEvents.Single());
    }

    [TestMethod]
    public async Task Handle_WhenBlobDeletionThrows_ShouldContinueAndSucceed()
    {
        var mediaFile = CreateApprovedMediaFileWithVariants();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));
        _storageService
            .Setup(s => s.DeleteFileByUrlAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("blob not found"));

        var result = await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess, "a missing blob must not abort the delete operation");
        _mediaRepo.Verify(r => r.DeleteAsync(mediaFile.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
