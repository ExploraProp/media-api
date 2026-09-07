using System.Net;
using ExploraProp.Marketing.Internal.ApiClient;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Services;
using SharedKernel.Kernel.Results;
using AuthId = Media.Domain.ValueObjects.Ids.AuthId;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Unit.Infrastructure.Services;

[TestClass]
public class MediaEventAccessServiceTests
{
    private readonly Mock<IMarketingInternalClient> _marketingClient = new();
    private readonly MediaEventAccessService _service;

    public MediaEventAccessServiceTests()
    {
        _service = new MediaEventAccessService(_marketingClient.Object);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenUserOwnsUserMedia_ShouldReturnSuccess()
    {
        var userId = AuthId.New();
        var mediaFile = CreateMediaFile(OwnerType.User, new OwnerId(userId.Value));

        var result = await _service.CanAccessEventsAsync(mediaFile, userId, null, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenUserDoesNotOwnUserMedia_ReturnsEventAccessDenied()
    {
        var ownerId = new OwnerId(Guid.NewGuid());
        var requesterId = AuthId.New();
        var mediaFile = CreateMediaFile(OwnerType.User, ownerId);

        var result = await _service.CanAccessEventsAsync(mediaFile, requesterId, null, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenListingPublisherMember_ShouldReturnSuccess()
    {
        var authId = AuthId.New();
        var clientUserId = new UploadedByClientId(Guid.NewGuid());
        var listingId = Guid.NewGuid();
        var mediaFile = CreateMediaFile(OwnerType.Listing, new OwnerId(listingId));

        _marketingClient
            .Setup(p => p.CanUserAccessListingResourcesAsync(clientUserId.Value, listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MarketingClientResult<bool>.Ok(true));

        var result = await _service.CanAccessEventsAsync(mediaFile, authId, clientUserId, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenListingNotFound_ShouldReturnNotFound()
    {
        var listingId = Guid.NewGuid();
        var authId = AuthId.New();
        var clientUserId = new UploadedByClientId(Guid.NewGuid());
        var mediaFile = CreateMediaFile(OwnerType.Listing, new OwnerId(listingId));

        _marketingClient
            .Setup(p => p.CanUserAccessListingResourcesAsync(clientUserId.Value, listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MarketingClientResult<bool>.Fail(HttpStatusCode.NotFound));

        var result = await _service.CanAccessEventsAsync(mediaFile, authId, clientUserId, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaFile)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenPublisherLookupFails_ShouldReturnEventAccessDenied()
    {
        var listingId = Guid.NewGuid();
        var authId = AuthId.New();
        var clientUserId = new UploadedByClientId(Guid.NewGuid());
        var mediaFile = CreateMediaFile(OwnerType.Listing, new OwnerId(listingId));

        _marketingClient
            .Setup(p => p.CanUserAccessListingResourcesAsync(clientUserId.Value, listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MarketingClientResult<bool>.Fail(HttpStatusCode.Forbidden));

        var result = await _service.CanAccessEventsAsync(mediaFile, authId, clientUserId, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenListingNonMember_ShouldReturnEventAccessDenied()
    {
        var authId = AuthId.New();
        var clientUserId = new UploadedByClientId(Guid.NewGuid());
        var listingId = Guid.NewGuid();
        var mediaFile = CreateMediaFile(OwnerType.Listing, new OwnerId(listingId));

        _marketingClient
            .Setup(p => p.CanUserAccessListingResourcesAsync(clientUserId.Value, listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MarketingClientResult<bool>.Fail(HttpStatusCode.Forbidden));

        var result = await _service.CanAccessEventsAsync(mediaFile, authId, clientUserId, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenListingWithoutClientUserId_ShouldReturnForbidden()
    {
        var mediaFile = CreateMediaFile(OwnerType.Listing, new OwnerId(Guid.NewGuid()));

        var result = await _service.CanAccessEventsAsync(mediaFile, AuthId.New(), null, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenOwnerTypeIsIdentification_ShouldReturnEventAccessDenied()
    {
        var mediaFile = CreateMediaFile(OwnerType.Identification, new OwnerId(Guid.NewGuid()));

        var result = await _service.CanAccessEventsAsync(mediaFile, AuthId.New(), null, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task CanAccessEventsAsync_WhenOwnerTypeIsVerificationSubmission_ShouldReturnEventAccessDenied()
    {
        var mediaFile = CreateMediaFile(OwnerType.VerificationSubmission, new OwnerId(Guid.NewGuid()));

        var result = await _service.CanAccessEventsAsync(mediaFile, AuthId.New(), null, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(MediaFileError.Permission.Code.Value, result.Error!.Code.Value);
    }

    private static MediaFile CreateMediaFile(OwnerType ownerType, OwnerId ownerId) =>
        MediaFile.Create(
            MediaFileId.New(),
            ownerId,
            ownerType,
            UploadMediaType.Photo,
            MediaStatus.Pending,
            StoragePath.FromDatabase("quarantine/test.jpg"),
            AllowedContentTypes.Jpeg,
            MediaFileSize.FromDatabase(100),
            UploadedByClientId.New()).Value!;
}
