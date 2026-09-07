using Media.Application.Commands;
using Media.Application.Services.Interfaces;
using Media.Application.UseCases;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
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
public class ApproveMediaUseCaseTests
{
    private readonly Mock<IMediaRepo> _mediaRepo = new();
    private readonly Mock<IMediaPolicyRepo> _policyRepo = new();
    private readonly Mock<IMediaPromotionService> _mediaPromotionService = new();
    private readonly Mock<ICatalogLabelLocalizer> _catalogLabels = new();
    private ApproveMediaUseCase _useCase = null!;

    private static readonly OwnerId OwnerId = new(Guid.NewGuid());
    private static readonly MediaFileId MediaFileId = MediaFileId.New();

    [TestInitialize]
    public void Setup()
    {
        _catalogLabels
            .Setup(l => l.Localize(It.IsAny<string>(), It.IsAny<CultureInfo?>()))
            .Returns<string, CultureInfo?>((code, _) => code);
        _useCase = new ApproveMediaUseCase(
            _mediaRepo.Object,
            _policyRepo.Object,
            _mediaPromotionService.Object,
            _catalogLabels.Object);
    }

    private static ApproveMediaCommand CreateCommand(MediaFileId? id = null) => new(id ?? MediaFileId);

    private static MediaFile CreateMediaFile(MediaFileId? id = null) =>
        MediaFile.Create(
            id ?? MediaFileId,
            OwnerId,
            OwnerType.Listing,
            UploadMediaType.Photo,
            MediaStatus.Scanning,
            StoragePath.FromDatabase($"quarantine/listing/{OwnerId.Value}/{Guid.NewGuid()}.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(1024),
            UploadedByClientId.New()).Value!;

    private static MediaPolicy CreateListingPolicy(AccessLevel accessLevel = AccessLevel.Public) =>
        MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            OwnerType.Listing,
            MediaMaxFileSize.FromDatabase(16 * 1024 * 1024),
            AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
            MediaMaxCount.FromDatabase(50),
            accessLevel,
            watermarkEnabled: true);

    private static IReadOnlyList<PromotedVariantInput> CreatePromotedVariants() =>
    [
        new PromotedVariantInput(
            VariantType.Original,
            VariantUrl.FromStorage("https://firebase.storage/listing/original.jpg"),
            null,
            null,
            MediaFileSize.FromDatabase(10)),
    ];

    private void SetupHappyPath(bool isPublic = true)
    {
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(CreateMediaFile()));

        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Success(CreateListingPolicy(
                isPublic ? AccessLevel.Public : AccessLevel.Private)));

        _mediaPromotionService
            .Setup(s => s.PromoteAsync(It.IsAny<MediaFile>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPromotionResult>.Success(
                new MediaPromotionResult(
                    "https://firebase.storage/listing/original.jpg",
                    CreatePromotedVariants(),
                    isPublic)));

        _mediaRepo
            .Setup(r => r.UpdateAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
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
    public async Task Handle_WithValidFile_ShouldCallUpdateAsync()
    {
        SetupHappyPath();

        await _useCase.Handle(CreateCommand(), CancellationToken.None);

        _mediaRepo.Verify(r => r.UpdateAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithValidFile_ShouldRaiseFileApprovedDomainEvent()
    {
        SetupHappyPath();
        MediaFile? savedMediaFile = null;
        _mediaRepo
            .Setup(r => r.UpdateAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .Callback<MediaFile, CancellationToken>((mediaFile, _) => savedMediaFile = mediaFile)
            .ReturnsAsync(Result<bool>.Success(true));

        await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsNotNull(savedMediaFile);
        Assert.AreEqual(1, savedMediaFile!.DomainEvents.Count);
        Assert.IsInstanceOfType<FileApproved>(savedMediaFile.DomainEvents.First());
    }

    [TestMethod]
    public async Task Handle_WhenMediaFileNotFound_ShouldPropagateNotFound()
    {
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Failure(SharedFailures.NotFound(nameof(MediaFile))));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaFile)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenPolicyNotFound_ShouldPropagateNotFound()
    {
        _mediaRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<MediaFileId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaFile>.Success(CreateMediaFile()));

        _policyRepo
            .Setup(r => r.GetByOwnerTypeAsync(OwnerType.Listing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPolicy>.Failure(SharedFailures.NotFound(nameof(MediaPolicy))));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaPolicy)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenPromotionFails_ShouldPropagateError()
    {
        SetupHappyPath();

        var promotionError = SharedFailures.SystemFailed;
        _mediaPromotionService
            .Setup(s => s.PromoteAsync(It.IsAny<MediaFile>(), It.IsAny<MediaPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MediaPromotionResult>.Failure(promotionError));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(promotionError.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task Handle_WhenPrivatePolicy_ShouldApproveWithoutPublicUrl()
    {
        SetupHappyPath(isPublic: false);
        MediaFile? savedMediaFile = null;
        _mediaRepo
            .Setup(r => r.UpdateAsync(It.IsAny<MediaFile>(), It.IsAny<CancellationToken>()))
            .Callback<MediaFile, CancellationToken>((mediaFile, _) => savedMediaFile = mediaFile)
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _useCase.Handle(CreateCommand(), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(savedMediaFile);
        var approvedEvent = savedMediaFile!.DomainEvents.OfType<FileApproved>().Single();
        Assert.IsNull(approvedEvent.PublicUrl);
    }
}
