using System.Collections.Concurrent;
using System.Net;
using ExploraProp.Marketing.Internal.ApiClient;

namespace Media.Functional.Infrastructure;

/// <summary>
/// In-memory Marketing ACL for Media functional SSE access checks.
/// Upload/delete do not call Marketing; only listing event access does.
/// </summary>
public sealed class FakeMarketingInternalClient : IMarketingInternalClient
{
    private readonly ConcurrentDictionary<Guid, HttpStatusCode> _listingAccess = new();

    public void Reset() => _listingAccess.Clear();

    public void AllowListingAccess(Guid listingId) =>
        _listingAccess[listingId] = HttpStatusCode.OK;

    public void DenyListingAccess(Guid listingId) =>
        _listingAccess[listingId] = HttpStatusCode.Forbidden;

    public void NotFoundListingAccess(Guid listingId) =>
        _listingAccess[listingId] = HttpStatusCode.NotFound;

    public Task<MarketingClientResult<bool>> CanUserAccessListingResourcesAsync(
        Guid clientUserId,
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        _ = clientUserId;
        if (!_listingAccess.TryGetValue(listingId, out var status))
            status = HttpStatusCode.OK;

        return Task.FromResult(status == HttpStatusCode.OK
            ? MarketingClientResult<bool>.Ok(true)
            : MarketingClientResult<bool>.Fail(status));
    }

    public Task<MarketingClientResult<ListingIndexDto>> GetListingByIdWithDetailsAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(MarketingClientResult<ListingIndexDto>.Fail(HttpStatusCode.NotFound));

    public Task<MarketingClientResult<IReadOnlyList<ListingIndexDto>>> GetListingsByPropertyIdAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            MarketingClientResult<IReadOnlyList<ListingIndexDto>>.Ok(
                Array.Empty<ListingIndexDto>()));

    public Task<MarketingClientResult<IReadOnlyList<ListingIndexDto>>> GetAllActiveWithActiveOffersAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            MarketingClientResult<IReadOnlyList<ListingIndexDto>>.Ok(
                Array.Empty<ListingIndexDto>()));
}
