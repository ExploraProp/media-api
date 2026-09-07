using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Media.Infrastructure.Services;

/// <summary>
/// In-process large-variant public URL service for Media mesh ACL.
/// </summary>
public sealed class MediaPublicUrlService(MediaDbContext context)
{
    public async Task<IReadOnlyDictionary<Guid, string?>> GetPublicUrlsAsync(
        IReadOnlyList<Guid> mediaFileIds,
        CancellationToken cancellationToken = default)
    {
        if (mediaFileIds.Count == 0)
            return new Dictionary<Guid, string?>();

        var ids = mediaFileIds.Select(id => new MediaFileId(id)).ToList();
        var files = await context.Set<MediaFile>()
            .Include(m => m.Variants)
            .Where(m => ids.Contains(m.Id) && m.Status == MediaStatus.Approved)
            .ToListAsync(cancellationToken);

        return files.ToDictionary(
            f => f.Id.Value,
            f => f.Variants.FirstOrDefault(v => v.VariantType == VariantType.Large)?.Url.Value
                ?? f.PublicUrl?.Value);
    }
}
