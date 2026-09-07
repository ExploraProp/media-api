using Media.Application.Dto;
using Media.Application.Queries;
using Media.Application.Services.Interfaces;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;
using AuthId = Media.Domain.ValueObjects.Ids.AuthId;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class WaitMediaFileEventUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IMediaEventAccessService> _mediaEventAccessService = new();
    private readonly Mock<IMediaEventWaitService> _mediaEventWaitService = new();
    private WaitMediaFileEventUseCase _useCase = null!;

    [TestInitialize]
    public void Setup() =>
        _useCase = new WaitMediaFileEventUseCase(
            _mediaRepo.Object,
            _mediaEventAccessService.Object,
            _mediaEventWaitService.Object);

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ShouldReturnNotFound()
    {
        var mediaFileId = MediaFileId.New();
        var userId = AuthId.New();

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));

        var result = await _useCase.Handle(
            new WaitMediaFileEventQuery(mediaFileId, userId, null),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaFile)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenAccessDenied_ShouldReturnForbidden()
    {
        var mediaFileId = MediaFileId.New();
        var userId = AuthId.New();
        var mediaFile = CreateMediaFile(mediaFileId);

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));
        _mediaEventAccessService
            .Setup(s => s.CanAccessEventsAsync(mediaFile, userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(MediaFileError.Permission));

        var result = await _useCase.Handle(
            new WaitMediaFileEventQuery(mediaFileId, userId, null),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenEventPublished_ShouldReturnEvent()
    {
        var mediaFileId = MediaFileId.New();
        var userId = AuthId.New();
        var mediaFile = CreateMediaFile(mediaFileId);
        var mediaEvent = new MediaFileEvent("fileApproved", new { mediaFileId = mediaFileId.Value });

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));
        _mediaEventAccessService
            .Setup(s => s.CanAccessEventsAsync(mediaFile, userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _mediaEventWaitService
            .Setup(s => s.WaitForTerminalEventAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaFileEventWaitDto(mediaEvent, IsTimeout: false));

        var result = await _useCase.Handle(
            new WaitMediaFileEventQuery(mediaFileId, userId, null),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.Value!.IsTimeout);
        Assert.AreEqual(mediaEvent, result.Value.Event);
    }

    private static MediaFile CreateMediaFile(MediaFileId mediaFileId) =>
        MediaFile.Create(
            mediaFileId,
            new OwnerId(Guid.NewGuid()),
            OwnerType.User,
            UploadMediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase("quarantine/user/id/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;
}
