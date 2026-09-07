using Media.Application.Commands;
using Media.Application.Services.Interfaces;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.Services.Errors;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;
using System.Globalization;
using System.Threading.Channels;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class UploadMediaUseCaseTests
{
    private readonly Mock<IMediaPolicyRepo> _policyRepo = new();
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<IMediaUploadFileService> _mediaUploadFileService = new();
    private readonly Mock<IImageProcessingService> _imageProcessingService = new();
    private readonly Channel<MediaFileId> _channel = Channel.CreateUnbounded<MediaFileId>();
    private readonly Mock<ICatalogLabelLocalizer> _catalogLabels = new();
    private UploadMediaUseCase _useCase = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());

    [TestInitialize]
    public void Setup()
    {
        _catalogLabels
            .Setup(l => l.Localize(It.IsAny<string>(), It.IsAny<CultureInfo?>()))
            .Returns<string, CultureInfo?>((code, _) => code);
        _useCase = new UploadMediaUseCase(
            _policyRepo.Object,
            _mediaRepo.Object,
            _storageService.Object,
            _mediaUploadFileService.Object,
            _imageProcessingService.Object,
            _channel,
            _catalogLabels.Object);
    }

    private static UploadMediaCommand CreateCommand(long fileLength = 1024)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(fileLength);
        file.Setup(f => f.FileName).Returns("photo.jpg");
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00]));

        return new UploadMediaCommand(
            OwnerType.Listing,
            OwnerId,
            MediaFileId.New(),
            UploadMediaType.Photo,
            file.Object,
            MediaFileSize.FromDatabase(fileLength > 0 ? fileLength : 1),
            UploadedByClientId.New());
    }

    private static MediaPolicy CreateListingPolicy() =>
        MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            OwnerType.Listing,
            MediaMaxFileSize.FromDatabase(16 * 1024 * 1024),
            AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
            MediaMaxCount.FromDatabase(50),
            AccessLevel.Public,
            watermarkEnabled: true);

    private void SetupHappyPath()
    {
        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(CreateListingPolicy()));

        _mediaRepo
            .Setup(r => r.CountByOwnerAsync(OwnerId.Value, OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(0));

        _mediaUploadFileService
            .Setup(s => s.PrepareAsync(It.IsAny<IFormFile>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00]);
                return Result<PreparedMediaUploadFile>.Success(
                    new PreparedMediaUploadFile(stream, AllowedContentTypes.Jpeg));
            });

        _imageProcessingService
            .Setup(s => s.StripExifAsync(It.IsAny<Stream>(), AllowedContentTypes.Jpeg, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream input, AllowedContentTypes _, CancellationToken _) =>
            {
                var output = new MemoryStream();
                input.CopyTo(output);
                output.Position = 0;
                return Result<Stream>.Success(output);
            });

        _storageService
            .Setup(s => s.UploadToQuarantineAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                OwnerId.Value,
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success($"quarantine/listing/{OwnerId.Value}/{Guid.NewGuid()}.jpg"));

        _mediaRepo
            .Setup(r => r.AddAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaFile entity, CancellationToken _) => Result<MediaFile>.Success(entity));
    }

    [TestMethod]
    public async Task Handle_WithValidFile_ShouldReturnPendingStatus()
    {
        SetupHappyPath();
        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(MediaStatus.Pending.ToI18nCode(), result.Value!.Status);
    }

    [TestMethod]
    public async Task Handle_WhenPolicyMissing_ShouldPropagateNotFound()
    {
        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Failure(SharedFailures.NotFound(nameof(MediaPolicy))));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaPolicy)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenFileTooLarge_ShouldReturnFileTooLargeError()
    {
        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(CreateListingPolicy()));

        _mediaUploadFileService
            .Setup(s => s.PrepareAsync(It.IsAny<IFormFile>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PreparedMediaUploadFile>.Failure(MediaPolicyError.TooLarge));

        var result = await _useCase.Handle(CreateCommand(fileLength: 20 * 1024 * 1024), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.TooLarge, result.Error!);
    }

    [TestMethod]
    public async Task Handle_WhenUnsupportedContentType_ShouldReturnUnsupportedContentTypeError()
    {
        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(CreateListingPolicy()));

        _mediaUploadFileService
            .Setup(s => s.PrepareAsync(It.IsAny<IFormFile>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PreparedMediaUploadFile>.Failure(MediaPolicyError.UnsupportedContentType));

        var command = CreateCommand();
        var result = await _useCase.Handle(command, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.UnsupportedContentType, result.Error!);
    }

    [TestMethod]
    public async Task Handle_WhenMaxMediaCountReached_ShouldReturnMaxMediaCountReachedError()
    {
        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(CreateListingPolicy()));

        _mediaRepo
            .Setup(r => r.CountByOwnerAsync(OwnerId.Value, OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(50));

        _mediaUploadFileService
            .Setup(s => s.PrepareAsync(It.IsAny<IFormFile>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00]);
                return Result<PreparedMediaUploadFile>.Success(
                    new PreparedMediaUploadFile(stream, AllowedContentTypes.Jpeg));
            });

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaPolicyError.TooMany.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenImageProcessingFails_ShouldReturnProcessingError()
    {
        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(CreateListingPolicy()));

        _mediaRepo
            .Setup(r => r.CountByOwnerAsync(OwnerId.Value, OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(0));

        _mediaUploadFileService
            .Setup(s => s.PrepareAsync(It.IsAny<IFormFile>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00]);
                return Result<PreparedMediaUploadFile>.Success(
                    new PreparedMediaUploadFile(stream, AllowedContentTypes.Jpeg));
            });

        _imageProcessingService
            .Setup(s => s.StripExifAsync(It.IsAny<Stream>(), AllowedContentTypes.Jpeg, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Stream>.Failure(ImageProcessingServiceError.Processing));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ImageProcessingServiceError.Processing.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenCountFails_PropagatesError()
    {
        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(CreateListingPolicy()));

        var error = SharedFailures.SystemFailed;
        _mediaRepo
            .Setup(r => r.CountByOwnerAsync(OwnerId.Value, OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure(error));

        _mediaUploadFileService
            .Setup(s => s.PrepareAsync(It.IsAny<IFormFile>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00]);
                return Result<PreparedMediaUploadFile>.Success(
                    new PreparedMediaUploadFile(stream, AllowedContentTypes.Jpeg));
            });

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error.Code.Value, result.Error!.Code.Value);
    }
}
