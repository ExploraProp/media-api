using Functional.Application;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Functional.Infrastructure;
using Media.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Functional.StepDefinitions.MediaUpload;

[Binding]
[Scope(Feature = "MediaUpload")]
[Scope(Feature = "MediaApproval")]
public sealed class MediaUploadBackgroundSteps(
    ScenarioContext scenarioContext,
    FeatureContext featureContext)
{
    private readonly ScenarioContext _scenarioContext = scenarioContext;
    private readonly Worker _worker = featureContext.Get<Worker>();

    [Given(@"seeds a listing owner id")]
    public async Task SeedsAListingOwnerId()
    {
        var listingId = Guid.NewGuid();
        _scenarioContext.Put(MediaUploadKeys.ListingOwnerId, listingId);
        _worker.Services.GetRequiredService<FakeMarketingInternalClient>()
            .AllowListingAccess(listingId);
        await EnsureListingMediaPolicyAsync();
    }

    private async Task EnsureListingMediaPolicyAsync()
    {
        await using var scope = _worker.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        if (await db.MediaPolicy.AnyAsync(p => p.OwnerType == OwnerType.Listing))
            return;

        // 8 MB / 50 photos — matches oversized (10 MB) and photo-limit scenarios.
        var policy = MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            OwnerType.Listing,
            MediaMaxFileSize.FromDatabase(8 * 1024 * 1024),
            AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
            MediaMaxCount.FromDatabase(50),
            AccessLevel.Public,
            watermarkEnabled: true);
        db.MediaPolicy.Add(policy);
        await db.SaveChangesAsync();
    }

    [Given(@"seeds the maximum number of listing photos")]
    public async Task SeedsTheMaximumNumberOfListingPhotos()
    {
        var listingId = _scenarioContext.Get(MediaUploadKeys.ListingOwnerId);
        await using var scope = _worker.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();

        for (var i = 0; i < 50; i++)
        {
            var mediaFile = MediaFile.Create(
                MediaFileId.New(),
                new OwnerId(listingId),
                OwnerType.Listing,
                UploadMediaType.Photo,
                MediaStatus.Pending,
                StoragePath.FromDatabase($"quarantine/listing/{listingId}/{Guid.NewGuid()}.jpg"),
                AllowedContentTypes.Jpeg,
                MediaFileSize.FromDatabase(1024),
                UploadedByClientId.New()).Value!;
            db.MediaFile.Add(mediaFile);
        }

        await db.SaveChangesAsync();
    }

    [Given(@"allows listing event access for the signed-in user")]
    public void AllowsListingEventAccessForTheSignedInUser()
    {
        var listingId = _scenarioContext.Get(MediaUploadKeys.ListingOwnerId);
        _worker.Services.GetRequiredService<FakeMarketingInternalClient>()
            .AllowListingAccess(listingId);
    }

    [Given(@"denies listing event access for the signed-in user")]
    public void DeniesListingEventAccessForTheSignedInUser()
    {
        var listingId = _scenarioContext.Get(MediaUploadKeys.ListingOwnerId);
        _worker.Services.GetRequiredService<FakeMarketingInternalClient>()
            .DenyListingAccess(listingId);
    }
}
