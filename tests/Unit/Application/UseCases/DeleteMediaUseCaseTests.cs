using Media.Application.Commands;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Unit.TestSupport;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class DeleteMediaUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IMediaUploadRepo> _mediaUploadRepo = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<ILogger<DeleteMediaUseCase>> _logger = new();
    private DeleteMediaUseCase _useCase = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());
    private static readonly UploadedByClientId UploaderId = UploadedByClientId.New();
    private static readonly UploadedByClientId OtherUserId = UploadedByClientId.New();

    [TestInitialize]
    public void Setup()
    {
        _useCase = new DeleteMediaUseCase(
            _mediaRepo.Object,
            _mediaUploadRepo.Object,
            _storageService.Object,
            _logger.Object);

        _storageService.Setup(s => s.DeleteFileAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _storageService.Setup(s => s.DeleteFileByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _mediaRepo
            .Setup(r => r.DeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _mediaUploadRepo
            .Setup(r => r.DeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));
    }

    private static MediaUpload CreatePendingUpload(UploadedByClientId? uploadedBy = null) =>
        MediaTestFactory.CreateUpload(ownerId: OwnerId, uploadedBy: uploadedBy ?? UploaderId);

    private static MediaUpload CreateScanningUpload(UploadedByClientId? uploadedBy = null) =>
        MediaTestFactory.CreateScanningUpload(ownerId: OwnerId, uploadedBy: uploadedBy ?? UploaderId);

    private static MediaFile CreateApprovedMediaFileWithVariants(UploadedByClientId? uploadedBy = null)
    {
        var variants = new List<PromotedVariantInput>
        {
            new(VariantType.Original,
                Link.FromDatabase("https://firebase.storage/listing/original.jpg"),
                null, MediaFileSize.FromDatabase(5000)),
            new(VariantType.Thumbnail,
                Link.FromDatabase("https://firebase.storage/listing/thumbnail.webp"),
                MediaVariantMetadata.ForImage(200, 200), MediaFileSize.FromDatabase(1000)),
        };

        return MediaTestFactory.CreateReadyMediaFile(
            ownerId: OwnerId,
            accessLevel: AccessLevel.Public,
            originalUrl: "https://firebase.storage/listing/original.jpg",
            variants: variants,
            uploadedBy: uploadedBy ?? UploaderId);
    }

    private static MediaFile CreateApprovedMediaFileWithPrivateVariant(UploadedByClientId? uploadedBy = null)
    {
        var variants = new List<PromotedVariantInput>
        {
            new(VariantType.Original,
                Link.FromDatabase("listing/owner/original.jpg"),
                null, MediaFileSize.FromDatabase(5000)),
        };

        return MediaTestFactory.CreateReadyMediaFile(
            ownerId: OwnerId,
            accessLevel: AccessLevel.Private,
            originalUrl: "listing/owner/original.jpg",
            variants: variants,
            uploadedBy: uploadedBy ?? UploaderId);
    }

    private void SetupUpload(MediaUpload upload)
    {
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(upload.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));
    }

    private void SetupReady(MediaFile mediaFile)
    {
        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));
    }

    private DeleteMediaCommand AsOwner(MediaFileId id) =>
        new(id, UploaderId, IsAdmin: false);

    private static DeleteMediaCommand AsAdmin(MediaFileId id) =>
        new(id, OtherUserId, IsAdmin: true);

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ShouldPropagateNotFound()
    {
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Failure(SharedFailures.NotFound(nameof(MediaUpload))));

        var result = await _useCase.Handle(
            AsAdmin(MediaFileId.New()), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaUpload)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenRequestingUserIsNeitherUploaderNorAdmin_ShouldReturnForbidden()
    {
        var upload = CreatePendingUpload(uploadedBy: UploaderId);
        SetupUpload(upload);

        var command = new DeleteMediaCommand(upload.Id, OtherUserId, IsAdmin: false);
        var result = await _useCase.Handle(command, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(HttpStatusCode.Forbidden, result.Error!.StatusCode);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenRequestingUserIsUploader_ShouldSucceed()
    {
        var upload = CreatePendingUpload(uploadedBy: UploaderId);
        SetupUpload(upload);

        var result = await _useCase.Handle(AsOwner(upload.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task Handle_WhenAdminDeletesAnotherUsersFile_ShouldSucceed()
    {
        var upload = CreatePendingUpload(uploadedBy: UploaderId);
        SetupUpload(upload);

        var result = await _useCase.Handle(AsAdmin(upload.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task Handle_WhenFileIsScanning_ShouldReturnOngoingScanning()
    {
        var upload = CreateScanningUpload();
        SetupUpload(upload);

        var result = await _useCase.Handle(AsOwner(upload.Id), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(HttpStatusCode.Conflict, result.Error!.StatusCode);
        Assert.AreEqual(MediaUploadError.OngoingScanning.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WithApprovedFile_ShouldDeleteVariantsByUrl()
    {
        var mediaFile = CreateApprovedMediaFileWithVariants();
        SetupReady(mediaFile);

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
        SetupReady(mediaFile);

        await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        _storageService.Verify(
            s => s.DeleteFileAsync(It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_WithPendingFile_ShouldDeleteQuarantineFile()
    {
        var upload = CreatePendingUpload();
        SetupUpload(upload);

        await _useCase.Handle(AsOwner(upload.Id), CancellationToken.None);

        _storageService.Verify(s => s.DeleteFileAsync(upload.QuarantinePath.Value), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithPrivateVariantUrl_ShouldDeleteFileByPath()
    {
        var mediaFile = CreateApprovedMediaFileWithPrivateVariant();
        SetupReady(mediaFile);

        await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        _storageService.Verify(
            s => s.DeleteFileAsync("listing/owner/original.jpg"),
            Times.Once);
        _storageService.Verify(
            s => s.DeleteFileByUrlAsync(It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_WithPendingUpload_ShouldCallUploadDeleteAsync()
    {
        var upload = CreatePendingUpload();
        SetupUpload(upload);

        var result = await _useCase.Handle(AsOwner(upload.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        _mediaUploadRepo.Verify(
            r => r.DeleteAsync(upload.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_AfterSuccessfulUploadDeletion_FileDeletedEventIsRaised()
    {
        var upload = CreatePendingUpload();
        SetupUpload(upload);

        await _useCase.Handle(AsOwner(upload.Id), CancellationToken.None);

        Assert.AreEqual(1, upload.DomainEvents.Count,
            "exactly one FileDeleted event must be queued on the aggregate for the EF interceptor to dispatch");
        Assert.IsInstanceOfType<FileDeleted>(upload.DomainEvents.Single());
    }

    [TestMethod]
    public async Task Handle_WhenBlobDeletionThrows_ShouldContinueAndSucceed()
    {
        var mediaFile = CreateApprovedMediaFileWithVariants();
        SetupReady(mediaFile);
        _storageService
            .Setup(s => s.DeleteFileByUrlAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("blob not found"));

        var result = await _useCase.Handle(AsOwner(mediaFile.Id), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess, "a missing blob must not abort the delete operation");
        _mediaRepo.Verify(r => r.DeleteAsync(mediaFile.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
