using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Repositories;

public interface IMediaRepo
{
    Task<Result<int>> CountByOwnerAsync(Guid ownerId, OwnerType ownerType, CancellationToken ct);
    Task<Result<MediaFile>> AddAsync(MediaFile mediaFile, CancellationToken ct);
    Task<Result<MediaFile>> GetByIdAsync(MediaFileId id, CancellationToken ct);
    Task<Result<bool>> UpdateAsync(MediaFile mediaFile, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(MediaFileId id, CancellationToken ct);
    Task<Result<IReadOnlyList<MediaFile>>> GetByStatusesAsync(IReadOnlyList<MediaStatus> statuses, CancellationToken ct);
    Task<Result<IReadOnlyList<MediaFile>>> GetOrphansAsync(DateTime pendingBefore, int maxCount, CancellationToken ct);
}
