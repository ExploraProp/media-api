using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Repositories;

public interface IMediaPolicyRepo
{
    Task<Result<MediaPolicy>> GetByOwnerTypeAsync(OwnerType ownerType, CancellationToken ct);
}
