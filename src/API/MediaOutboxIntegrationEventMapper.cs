using IntegrationEvents;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
using SharedKernel.Kernel.IntegrationEvents;

namespace Media.API;

public sealed class MediaOutboxIntegrationEventMapper : IOutboxIntegrationEventMapper
{
    public Task<IReadOnlyList<object>> MapToIntegrationEventsAsync(
        object domainEvent,
        CancellationToken cancellationToken = default)
    {
        switch (domainEvent)
        {
            case FileApproved e:
                {
                    var url = ResolvePrimaryMediaUrl(e);
                    var events = new List<object>
                {
                    new MediaFileApprovedV1(
                        e.OwnerType.ToI18nCode(),
                        e.OwnerId.Value,
                        e.MediaFileId.Value,
                        url),
                };
                    if (e.OwnerType == OwnerType.Listing)
                        events.Add(new ListingPrimaryMediaApprovedV1(e.OwnerId.Value, url));
                    return Task.FromResult<IReadOnlyList<object>>(events);
                }
            case FileRejected e:
                return Task.FromResult<IReadOnlyList<object>>(
                [
                    new MediaFileRejectedV1(
                        e.OwnerType.ToI18nCode(),
                        e.OwnerId.Value,
                        e.MediaFileId.Value)
                ]);
            default:
                return Task.FromResult<IReadOnlyList<object>>([]);
        }
    }

    private static string? ResolvePrimaryMediaUrl(FileApproved notification)
    {
        if (!string.IsNullOrWhiteSpace(notification.PublicUrl))
            return notification.PublicUrl;

        return notification.Variants
            .FirstOrDefault(v => string.Equals(v.Type, "large", StringComparison.OrdinalIgnoreCase))
            ?.Url;
    }
}
