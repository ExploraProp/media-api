namespace Media.Application.Services.Interfaces;

/// <summary>
/// Resolves public URLs for approved media files (internal ACL).
/// </summary>
public interface IMediaPublicUrlService
{
    Task<IReadOnlyDictionary<Guid, string?>> GetPublicUrlsAsync(
        IReadOnlyList<Guid> mediaFileIds,
        CancellationToken cancellationToken = default);
}
