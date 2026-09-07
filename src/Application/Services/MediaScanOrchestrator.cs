using Media.Application.Commands;
using Media.Application.Services.Interfaces;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using SharedKernel.Kernel.Results;

namespace Media.Application.Services;

public sealed class MediaScanOrchestrator(IMediator mediator) : IMediaScanOrchestrator
{
    public async Task<Result<bool>> ApproveAsync(MediaFileId id, CancellationToken ct)
    {
        var result = await mediator.Send(new ApproveMediaCommand(id), ct);
        return result.IsSuccess
            ? Result<bool>.Success(true)
            : Result<bool>.FromResult(result);
    }

    public Task<Result<bool>> RejectAsync(MediaFileId id, ScanRejectionReason reason, CancellationToken ct) =>
        mediator.Send(new RejectMediaCommand(id, reason), ct);
}
