namespace Media.Application.Dto;

/// <summary>
/// Wire shape for public URL resolve
/// (<c>POST api/v1/internal/media/public-urls</c>).
/// </summary>
public sealed record PublicUrlsRequest(IReadOnlyList<Guid> MediaFileIds);

public sealed record PublicUrlsResponse(IReadOnlyDictionary<Guid, string?> Urls);
