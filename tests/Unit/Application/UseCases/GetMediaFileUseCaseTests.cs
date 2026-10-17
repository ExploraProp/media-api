using Media.Application.Queries;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Unit.TestSupport;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;
using System.Globalization;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class GetMediaFileUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IMediaUploadRepo> _mediaUploadRepo = new();
    private readonly Mock<IMediaPolicyRepo> _mediaPolicyRepo = new();
    private readonly Mock<ICatalogLabelLocalizer> _catalogLabels = new();
    private GetMediaFileUseCase _useCase = null!;

    private static MediaPolicy CreatePolicy(OwnerType ownerType, AccessLevel accessLevel) =>
        MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            ownerType,
            MediaMaxFileSize.FromDatabase(4 * 1024 * 1024),
            AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
            MediaMaxCount.FromDatabase(5),
            accessLevel,
            watermarkEnabled: false);

    [TestInitialize]
    public void Setup()
    {
        _catalogLabels
            .Setup(l => l.Localize(It.IsAny<string>(), It.IsAny<CultureInfo?>()))
            .Returns<string, CultureInfo?>((code, _) => code);
        _useCase = new GetMediaFileUseCase(
            _mediaRepo.Object,
            _mediaUploadRepo.Object,
            _mediaPolicyRepo.Object,
            _catalogLabels.Object);
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileExists_ShouldReturnPendingStatus()
    {
        var mediaFileId = MediaFileId.New();
        var upload = MediaTestFactory.CreateUpload(id: mediaFileId);

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(upload));

        var result = await _useCase.Handle(new GetMediaFileQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(MediaStatus.Pending.ToI18nCode(), result.Value!.Status);
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ShouldReturnNotFound()
    {
        var mediaFileId = MediaFileId.New();
        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Failure(SharedFailures.NotFound(nameof(MediaUpload))));

        var result = await _useCase.Handle(new GetMediaFileQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaUpload)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileIsApprovedAndPrivate_ReturnsForbiddenWithPrivateError()
    {
        var mediaFileId = MediaFileId.New();
        var mediaFile = MediaTestFactory.CreateReadyMediaFile(
            id: mediaFileId,
            ownerType: OwnerType.Identification,
            accessLevel: AccessLevel.Private,
            originalUrl: "private/path/original.jpg");

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));
        _mediaPolicyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Identification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(
                CreatePolicy(OwnerType.Identification, AccessLevel.Private)));

        var result = await _useCase.Handle(new GetMediaFileQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.Private.Code.Value, result.Error!.Code.Value);
    }
}
