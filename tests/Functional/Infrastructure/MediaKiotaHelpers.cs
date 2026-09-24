using Microsoft.Kiota.Abstractions;

namespace Media.Functional.Infrastructure;

/// <summary>
/// Shared helpers for Media own-BC Kiota calls in functional steps.
/// </summary>
internal static class MediaKiotaHelpers
{
    public static MultipartBody BuildUploadBody(
        Stream fileStream,
        string fileName,
        string contentType,
        string ownerType,
        Guid ownerId,
        string mediaType = "photo")
    {
        var body = new MultipartBody();
        body.AddOrReplacePart("OwnerType", "text/plain", ownerType);
        body.AddOrReplacePart("OwnerId", "text/plain", ownerId.ToString());
        body.AddOrReplacePart("MediaType", "text/plain", mediaType);
        body.AddOrReplacePart("File", contentType, fileStream, fileName);
        return body;
    }
}
