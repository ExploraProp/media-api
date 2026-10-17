using Media.Application.Commands;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using MediatR;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Application.UseCases;

public sealed class RejectMediaUseCase(
    IMediaUploadRepo mediaUploadRepo,
    IStorageService storageService) : IRequestHandler<RejectMediaCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RejectMediaCommand command, CancellationToken cancellationToken)
    {
        var uploadResult = await mediaUploadRepo.GetByIdAsync(command.Id, cancellationToken);
        if (uploadResult.IsFailure) return Result<bool>.FromResult(uploadResult);
        var upload = uploadResult.Value!;

        await storageService.DeleteFileAsync(upload.QuarantinePath.Value);

        var reason = command.Reason.ToI18nCode();
        var transitionResult = command.Reason == ScanRejectionReason.Infected
            ? upload.Reject(reason)
            : upload.MarkFailed(reason);
        if (transitionResult.IsFailure) return Result<bool>.FromResult(transitionResult);

        return await mediaUploadRepo.UpdateAsync(upload, cancellationToken);
    }
}
