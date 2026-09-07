using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using SharedKernel.Kernel.Results;

namespace Media.Application.Services.Interfaces;

public interface IMediaScanOrchestrator
{
    Task<Result<bool>> ApproveAsync(MediaFileId id, CancellationToken ct);

    Task<Result<bool>> RejectAsync(MediaFileId id, ScanRejectionReason reason, CancellationToken ct);
}
