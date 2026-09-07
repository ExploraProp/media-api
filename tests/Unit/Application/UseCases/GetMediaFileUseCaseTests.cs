using Media.Application.Queries;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;
using System.Globalization;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class GetMediaFileUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<ICatalogLabelLocalizer> _catalogLabels = new();
    private GetMediaFileUseCase _useCase = null!;

    [TestInitialize]
    public void Setup()
    {
        _catalogLabels
            .Setup(l => l.Localize(It.IsAny<string>(), It.IsAny<CultureInfo?>()))
            .Returns<string, CultureInfo?>((code, _) => code);
        _useCase = new GetMediaFileUseCase(_mediaRepo.Object, _catalogLabels.Object);
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileExists_ShouldReturnPendingStatus()
    {
        var mediaFileId = MediaFileId.New();
        var mediaFile = MediaFile.Create(
            mediaFileId,
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            UploadMediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase("quarantine/listing/id/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

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

        var result = await _useCase.Handle(new GetMediaFileQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaFile)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileIsApprovedAndPrivate_ReturnsForbiddenWithPrivateError()
    {
        var mediaFileId = MediaFileId.New();
        var mediaFile = MediaFile.Create(
            mediaFileId,
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            UploadMediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase("quarantine/listing/id/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;
        mediaFile.BeginScan();
        mediaFile.Approve(null, AccessLevel.Private, []);

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(new GetMediaFileQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Private.Code.Value, result.Error!.Code.Value);
    }
}
