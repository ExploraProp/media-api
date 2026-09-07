namespace Media.Application.Dto;

/// <summary>
/// Wire shape for mesh ACL public URL resolve
/// (<c>POST api/v1/internal/media/public-urls</c>).
/// </summary>
public sealed record MeshPublicUrlsRequest(IReadOnlyList<Guid> MediaFileIds);

public sealed record MeshPublicUrlsResponse(IReadOnlyDictionary<Guid, string?> Urls);
