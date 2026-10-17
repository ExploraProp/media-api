using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Repositories;

public interface IMediaUploadRepo
{
    Task<Result<int>> CountInFlightByOwnerAsync(Guid ownerId, OwnerType ownerType, CancellationToken ct);
    Task<Result<MediaUpload>> AddAsync(MediaUpload upload, CancellationToken ct);
    Task<Result<MediaUpload>> GetByIdAsync(MediaFileId id, CancellationToken ct);
    Task<Result<bool>> UpdateAsync(MediaUpload upload, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(MediaFileId id, CancellationToken ct);
    Task<Result<bool>> HardDeleteAsync(MediaFileId id, CancellationToken ct);
    Task<Result<IReadOnlyList<MediaUpload>>> GetByStatusesAsync(IReadOnlyList<MediaStatus> statuses, CancellationToken ct);
    Task<Result<IReadOnlyList<MediaUpload>>> GetOrphansAsync(DateTime pendingBefore, int maxCount, CancellationToken ct);
}
