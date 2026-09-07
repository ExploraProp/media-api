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

public sealed class MediaRepo(MediaDbContext context, ILogger<MediaRepo> logger)
    : BaseRepo<MediaFile, MediaFileId>(context, logger), IMediaRepo
{
    public async Task<Result<int>> CountByOwnerAsync(Guid ownerId, OwnerType ownerType, CancellationToken ct)
    {
        try
        {
            var owner = new OwnerId(ownerId);
            var count = await Set.CountAsync(m => m.OwnerId == owner && m.OwnerType == ownerType, ct);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error counting media files for owner {OwnerId} with type {OwnerType}", ownerId, ownerType);
            return Result<int>.Failure(SharedFailures.SystemFailed);
        }
    }

    public override async Task<Result<MediaFile>> GetByIdAsync(MediaFileId id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await Set
                .Include(m => m.Variants)
                .FirstOrDefaultAsync(e => e.Id.Equals(id) && e.DeletedAt == null, cancellationToken);
            if (entity == null)
                return Result<MediaFile>.Failure(SharedFailures.NotFound(EntityName));
            return Result<MediaFile>.Success(entity);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting entity by id: {EntityName}", EntityName);
            return Result<MediaFile>.Failure(SharedFailures.SystemFailed);
        }
    }

    async Task<Result<MediaFile>> IMediaRepo.GetByIdAsync(MediaFileId id, CancellationToken ct) =>
        await GetByIdAsync(id, ct);

    async Task<Result<MediaFile>> IMediaRepo.AddAsync(MediaFile mediaFile, CancellationToken ct) =>
        await AddAsync(mediaFile, ct);

    async Task<Result<bool>> IMediaRepo.UpdateAsync(MediaFile mediaFile, CancellationToken ct) =>
        await UpdateAsync(mediaFile, ct);

    async Task<Result<bool>> IMediaRepo.DeleteAsync(MediaFileId id, CancellationToken ct) =>
        await DeleteAsync(id, ct);

    public async Task<Result<IReadOnlyList<MediaFile>>> GetByStatusesAsync(IReadOnlyList<MediaStatus> statuses, CancellationToken ct)
    {
        try
        {
            var entities = await Set
                .Include(m => m.Variants)
                .Where(m => statuses.Contains(m.Status) && m.DeletedAt == null)
                .ToListAsync(ct);
            return Result<IReadOnlyList<MediaFile>>.Success(entities);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting media files by statuses");
            return Result<IReadOnlyList<MediaFile>>.Failure(SharedFailures.SystemFailed);
        }
    }

    public async Task<Result<IReadOnlyList<MediaFile>>> GetOrphansAsync(DateTime pendingBefore, int maxCount, CancellationToken ct)
    {
        try
        {
            var entities = await Set
                .Include(m => m.Variants)
                .Where(m => m.DeletedAt == null &&
                            ((m.Status == MediaStatus.Pending && m.UploadedAt < pendingBefore) ||
                             m.Status == MediaStatus.Failed))
                .Take(maxCount)
                .ToListAsync(ct);
            return Result<IReadOnlyList<MediaFile>>.Success(entities);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting orphan media files");
            return Result<IReadOnlyList<MediaFile>>.Failure(SharedFailures.SystemFailed);
        }
    }

    protected override Result<Expression<Func<MediaFile, object>>> GetSortProperty(string propertyName) =>
        Result<Expression<Func<MediaFile, object>>>.Failure(QueryFailures.InvalidSortProperty(propertyName));

    protected override Result<Expression<Func<MediaFile, bool>>> GetFilterProperty(
        string propertyName,
        FilterOperator @operator,
        string value) =>
        Result<Expression<Func<MediaFile, bool>>>.Failure(QueryFailures.InvalidFilterProperty(propertyName, @operator.ToString()));
}
