using Media.Application.Queries;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Application.Services;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class GenerateSignedUrlUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IStorageService> _storageService = new();
    private GenerateSignedUrlUseCase _useCase = null!;

    [TestInitialize]
    public void Setup() => _useCase = new GenerateSignedUrlUseCase(_mediaRepo.Object, _storageService.Object);

    private static MediaFile CreatePendingFile(MediaFileId id) =>
        MediaFile.Create(
            id,
            new OwnerId(Guid.NewGuid()),
            OwnerType.Listing,
            UploadMediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase("quarantine/listing/id/file.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

    [TestMethod]
    public async Task Handle_WhenMediaFileNotApproved_ReturnsNotApprovedError()
    {
        var mediaFileId = MediaFileId.New();
        var mediaFile = CreatePendingFile(mediaFileId);

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(new GenerateSignedUrlQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.NotApproved.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileIsPublic_ReturnsForbiddenWithPublicError()
    {
        var mediaFileId = MediaFileId.New();
        var mediaFile = CreatePendingFile(mediaFileId);
        mediaFile.BeginScan();
        mediaFile.Approve("https://example.com/file.jpg", AccessLevel.Public, []);

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));

        var result = await _useCase.Handle(new GenerateSignedUrlQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Public.Code.Value, result.Error!.Code.Value);
    }
}
