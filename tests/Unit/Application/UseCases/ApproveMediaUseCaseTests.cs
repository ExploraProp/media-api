using Media.Application.Commands;
using Media.Application.Services.Interfaces;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Unit.TestSupport;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;
using System.Globalization;

namespace Media.Unit.Application.UseCases;

[TestClass]
public class ApproveMediaUseCaseTests
{
    private readonly Mock<IMediaUploadRepo> _mediaUploadRepo = new();
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IMediaPolicyRepo> _mediaPolicyRepo = new();
    private readonly Mock<IMediaPromotionService> _mediaPromotionService = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<ICatalogLabelLocalizer> _catalogLabels = new();
    private ApproveMediaUseCase _useCase = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());
    private static readonly MediaFileId MediaFileId = MediaFileId.New();

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
    public void Setup()
    {
        _catalogLabels
            .Setup(l => l.Localize(It.IsAny<string>(), It.IsAny<CultureInfo?>()))
            .Returns<string, CultureInfo?>((code, _) => code);
        _storageService
            .Setup(s => s.DeleteFileAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _useCase = new ApproveMediaUseCase(
            _mediaUploadRepo.Object,
            _mediaRepo.Object,
            _mediaPolicyRepo.Object,
            _mediaPromotionService.Object,
            _storageService.Object,
            _catalogLabels.Object);
    }

    private static ApproveMediaCommand CreateCommand(MediaFileId? id = null) => new(id ?? MediaFileId);

    private static MediaUpload CreateScanningUpload(
        MediaFileId? id = null,
        OwnerType ownerType = OwnerType.Listing) =>
        MediaTestFactory.CreateScanningUpload(id ?? MediaFileId, OwnerId, ownerType);

    private static IReadOnlyList<PromotedVariantInput> CreatePromotedVariants() =>
    [
        new PromotedVariantInput(
            VariantType.Original,
            Link.FromDatabase("https://firebase.storage/listing/original.jpg"),
            null,
            MediaFileSize.FromDatabase(10)),
    ];

    private void SetupHappyPath(OwnerType ownerType = OwnerType.Listing, bool isPublic = true)
    {
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Success(CreateScanningUpload(ownerType: ownerType)));

        _mediaPolicyRepo
            .Setup(r => r.GetByOwnerTypeAsync(ownerType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(
                CreatePolicy(ownerType, isPublic ? AccessLevel.Public : AccessLevel.Private)));

        _mediaPromotionService
            .Setup(s => s.PromoteAsync(It.IsAny<MediaUpload>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPromotionResult>.Success(
                new MediaPromotionResult(
                    isPublic ? "https://firebase.storage/listing/original.jpg" : "private/path/original.jpg",
                    CreatePromotedVariants(),
                    isPublic)));

        _mediaRepo
            .Setup(r => r.AddAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaFile entity, CancellationToken _) => Result<MediaFile>.Success(entity));

        _mediaUploadRepo
            .Setup(r => r.HardDeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
    }

    [TestMethod]
    public async Task Handle_WithValidFile_ShouldReturnApprovedStatus()
    {
        SetupHappyPath();

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(MediaStatus.Approved.ToI18nCode(), result.Value!.Status);
    }

    [TestMethod]
    public async Task Handle_WithValidFile_ShouldCallAddAsync()
    {
        SetupHappyPath();

        await _useCase.Handle(CreateCommand(), CancellationToken.None);

        _mediaRepo.Verify(r => r.AddAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediaUploadRepo.Verify(r => r.HardDeleteAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithValidFile_ShouldRaiseFileApprovedDomainEvent()
    {
        SetupHappyPath();
        MediaFile? savedMediaFile = null;
        _mediaRepo
            .Setup(r => r.AddAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .Callback<MediaFile, CancellationToken>((mediaFile, _) => savedMediaFile = mediaFile)
            .ReturnsAsync((MediaFile entity, CancellationToken _) => Result<MediaFile>.Success(entity));

        await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsNotNull(savedMediaFile);
        Assert.AreEqual(1, savedMediaFile!.DomainEvents.Count);
        Assert.IsInstanceOfType<FileApproved>(savedMediaFile.DomainEvents.First());
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ShouldPropagateNotFound()
    {
        _mediaUploadRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaUpload>.Failure(SharedFailures.NotFound(nameof(MediaUpload))));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaUpload)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenPromotionFails_ShouldPropagateError()
    {
        SetupHappyPath();

        var promotionError = SharedFailures.SystemFailed;
        _mediaPromotionService
            .Setup(s => s.PromoteAsync(It.IsAny<MediaUpload>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPromotionResult>.Failure(promotionError));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(promotionError.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenPrivatePolicy_ShouldApproveWithoutPublicUrl()
    {
        SetupHappyPath(ownerType: OwnerType.Identification, isPublic: false);
        MediaFile? savedMediaFile = null;
        _mediaRepo
            .Setup(r => r.AddAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .Callback<MediaFile, CancellationToken>((mediaFile, _) => savedMediaFile = mediaFile)
            .ReturnsAsync((MediaFile entity, CancellationToken _) => Result<MediaFile>.Success(entity));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(savedMediaFile);
        var approvedEvent = savedMediaFile!.DomainEvents.OfType<FileApproved>().Single();
        Assert.IsNull(approvedEvent.PublicUrl);
    }
}
