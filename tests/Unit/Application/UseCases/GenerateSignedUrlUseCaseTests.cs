using Media.Application.Queries;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Unit.TestSupport;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class GenerateSignedUrlUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IMediaPolicyRepo> _mediaPolicyRepo = new();
    private readonly Mock<IStorageService> _storageService = new();
    private GenerateSignedUrlUseCase _useCase = null!;

    private static MediaPolicy CreatePolicy(OwnerType ownerType, AccessLevel accessLevel) =>
        MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            ownerType,
            MediaMaxFileSize.FromDatabase(16 * 1024 * 1024),
            AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
            MediaMaxCount.FromDatabase(50),
            accessLevel,
            watermarkEnabled: true);

    [TestInitialize]
    public void Setup() =>
        _useCase = new GenerateSignedUrlUseCase(
            _mediaRepo.Object,
            _mediaPolicyRepo.Object,
            _storageService.Object);

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ReturnsNotFoundError()
    {
        var mediaFileId = MediaFileId.New();

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(
                SharedFailures.NotFound(nameof(MediaFile))));

        var result = await _useCase.Handle(new GenerateSignedUrlQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaFile)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileIsPublic_ReturnsForbiddenWithPublicError()
    {
        var mediaFileId = MediaFileId.New();
        var mediaFile = MediaTestFactory.CreateReadyMediaFile(
            id: mediaFileId,
            ownerType: OwnerType.Listing,
            accessLevel: AccessLevel.Public,
            originalUrl: "https://example.com/file.jpg");

        _mediaRepo
            .Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(mediaFile));
        _mediaPolicyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(
                CreatePolicy(OwnerType.Listing, AccessLevel.Public)));

        var result = await _useCase.Handle(new GenerateSignedUrlQuery(mediaFileId), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.Public.Code.Value, result.Error!.Code.Value);
    }
}
