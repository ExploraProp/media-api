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
using Media.Unit.TestSupport;
using SharedKernel.Kernel.Results;
using AuthId = Media.Domain.ValueObjects.Ids.AuthId;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class WaitMediaFileEventUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IMediaUploadRepo> _mediaUploadRepo = new();
    private readonly Mock<IMediaPolicyRepo> _mediaPolicyRepo = new();
    private readonly Mock<IMediaEventAccessService> _mediaEventAccessService = new();
    private readonly Mock<IMediaEventWaitService> _mediaEventWaitService = new();
    private WaitMediaFileEventUseCase _useCase = null!;

    [TestInitialize]
    public void Setup() =>
        _useCase = new WaitMediaFileEventUseCase(
            _mediaRepo.Object,
            _mediaUploadRepo.Object,
            _mediaPolicyRepo.Object,
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
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Failure(SharedFailures.NotFound(nameof(MediaUpload))));

        var result = await _useCase.Handle(
            new WaitMediaFileEventQuery(mediaFileId, userId, null),
            CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaUpload)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenAccessDenied_ShouldReturnForbidden()
    {
        var mediaFileId = MediaFileId.New();
        var userId = AuthId.New();
        var upload = MediaTestFactory.CreateUpload(id: mediaFileId, ownerType: OwnerType.User);

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));
        _mediaEventAccessService
            .Setup(s => s.CanAccessEventsAsync(
                upload.OwnerId,
                upload.OwnerType,
                userId,
                null,
                It.IsAny<CancellationToken>()))
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
        var upload = MediaTestFactory.CreateUpload(id: mediaFileId, ownerType: OwnerType.User);
        var mediaEvent = new MediaFileEvent("fileApproved", new { mediaFileId = mediaFileId.Value });

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));
        _mediaEventAccessService
            .Setup(s => s.CanAccessEventsAsync(
                upload.OwnerId,
                upload.OwnerType,
                userId,
                null,
                It.IsAny<CancellationToken>()))
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
}
