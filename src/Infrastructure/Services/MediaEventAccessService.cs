using ExploraProp.Marketing.Internal.ApiClient;
using Media.Application.Services;
using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results;
using AuthId = Media.Domain.ValueObjects.Ids.AuthId;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Infrastructure.Services;

public sealed class MediaEventAccessService(
    IMarketingInternalClient marketingClient) : IMediaEventAccessService
{
    public async Task<Result<bool>> CanAccessEventsAsync(
        MediaFile mediaFile,
        AuthId authId,
        UploadedByClientId? clientUserId,
        CancellationToken ct)
    {
        return mediaFile.OwnerType switch
        {
            OwnerType.User => mediaFile.OwnerId.Value == authId.Value
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(MediaFileError.Permission),

            OwnerType.Listing => await CanAccessListingEventsAsync(mediaFile, clientUserId, ct),

            OwnerType.Identification or OwnerType.VerificationSubmission =>
                Result<bool>.Failure(MediaFileError.Permission),

            _ => Result<bool>.Failure(MediaFileError.Permission),
        };
    }

    private async Task<Result<bool>> CanAccessListingEventsAsync(
        MediaFile mediaFile,
        UploadedByClientId? clientUserId,
        CancellationToken ct)
    {
        if (clientUserId is null)
            return Result<bool>.Failure(MediaFileError.Permission);

        var accessResult = (await marketingClient.CanUserAccessListingResourcesAsync(
            clientUserId.Value,
            mediaFile.OwnerId.Value,
            ct)).ToListingAccessResult();

        if (accessResult.IsSuccess)
            return accessResult;

        if (accessResult.Error!.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Result<bool>.Failure(SharedFailures.NotFound(nameof(MediaFile)));

        if (accessResult.Error.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return Result<bool>.Failure(MediaFileError.Permission);

        return Result<bool>.FromResult(accessResult);
    }
}
