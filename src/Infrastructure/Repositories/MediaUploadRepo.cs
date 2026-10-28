using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Filters;
using SharedKernel.Infrastructure.Repositories.Base;
using SharedKernel.Kernel.Results;
using System.Linq.Expressions;

namespace Media.Infrastructure.Repositories;

public sealed class MediaUploadRepo(MediaDbContext context, ILogger<MediaUploadRepo> logger)
    : BaseRepo<MediaUpload, MediaFileId>(context, logger), IMediaUploadRepo
{
    public async Task<Result<int>> CountInFlightByOwnerAsync(Guid ownerId, OwnerType ownerType, CancellationToken ct)
    {
        try
        {
            var owner = new OwnerId(ownerId);
            var count = await Set.CountAsync(
                m => m.OwnerId == owner
                     && m.OwnerType == ownerType
                     && (m.Status == MediaStatus.Pending || m.Status == MediaStatus.Scanning),
                ct);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error counting in-flight uploads for owner {OwnerId} with type {OwnerType}", ownerId, ownerType);
            return Result<int>.Failure(SharedFailures.SystemFailed);
        }
    }

    public override async Task<Result<MediaUpload>> GetByIdAsync(MediaFileId id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await Set
                .FirstOrDefaultAsync(e => e.Id.Equals(id) && e.DeletedAt == null, cancellationToken);
            if (entity == null)
                return Result<MediaUpload>.Failure(SharedFailures.NotFound(EntityName));
            return Result<MediaUpload>.Success(entity);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting entity by id: {EntityName}", EntityName);
            return Result<MediaUpload>.Failure(SharedFailures.SystemFailed);
        }
    }

    async Task<Result<MediaUpload>> IMediaUploadRepo.GetByIdAsync(MediaFileId id, CancellationToken ct) =>
        await GetByIdAsync(id, ct);

    async Task<Result<MediaUpload>> IMediaUploadRepo.AddAsync(MediaUpload upload, CancellationToken ct) =>
        await AddAsync(upload, ct);

    async Task<Result<bool>> IMediaUploadRepo.UpdateAsync(MediaUpload upload, CancellationToken ct) =>
        await UpdateAsync(upload, ct);

    async Task<Result<bool>> IMediaUploadRepo.DeleteAsync(MediaFileId id, CancellationToken ct) =>
        await DeleteAsync(id, ct);

    async Task<Result<bool>> IMediaUploadRepo.HardDeleteAsync(MediaFileId id, CancellationToken ct) =>
        await HardDeleteAsync(id, ct);

    public async Task<Result<IReadOnlyList<MediaUpload>>> GetByStatusesAsync(IReadOnlyList<MediaStatus> statuses, CancellationToken ct)
    {
        try
        {
            var entities = await Set
                .Where(m => statuses.Contains(m.Status) && m.DeletedAt == null)
                .ToListAsync(ct);
            return Result<IReadOnlyList<MediaUpload>>.Success(entities);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting media uploads by statuses");
            return Result<IReadOnlyList<MediaUpload>>.Failure(SharedFailures.SystemFailed);
        }
    }

    public async Task<Result<IReadOnlyList<MediaUpload>>> GetOrphansAsync(DateTime pendingBefore, int maxCount, CancellationToken ct)
    {
        try
        {
            var entities = await Set
                .Where(m => m.DeletedAt == null &&
                            ((m.Status == MediaStatus.Pending && m.UploadedAt < pendingBefore) ||
                             m.Status == MediaStatus.Failed))
                .Take(maxCount)
                .ToListAsync(ct);
            return Result<IReadOnlyList<MediaUpload>>.Success(entities);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting orphan media uploads");
            return Result<IReadOnlyList<MediaUpload>>.Failure(SharedFailures.SystemFailed);
        }
    }

    protected override Result<Expression<Func<MediaUpload, object>>> GetSortProperty(string propertyName) =>
        Result<Expression<Func<MediaUpload, object>>>.Failure(QueryFailures.InvalidSortProperty(propertyName));

    protected override Result<Expression<Func<MediaUpload, bool>>> GetFilterProperty(
        string propertyName,
        FilterOperator @operator,
        string value) =>
        Result<Expression<Func<MediaUpload, bool>>>.Failure(QueryFailures.InvalidFilterProperty(propertyName, @operator.ToString()));
}
