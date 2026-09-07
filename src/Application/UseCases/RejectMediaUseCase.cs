using Media.Application.Commands;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using MediatR;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Application.UseCases;

public sealed class RejectMediaUseCase(
    IMediaRepo mediaRepo,
    IStorageService storageService) : IRequestHandler<RejectMediaCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RejectMediaCommand command, CancellationToken cancellationToken)
    {
        var mediaFileResult = await mediaRepo.GetByIdAsync(command.Id, cancellationToken);
        if (mediaFileResult.IsFailure) return Result<bool>.FromResult(mediaFileResult);
        var mediaFile = mediaFileResult.Value!;

        await storageService.DeleteFileAsync(mediaFile.QuarantinePath.Value);

        var reason = command.Reason.ToI18nCode();
        var transitionResult = command.Reason == ScanRejectionReason.Infected
            ? mediaFile.Reject(reason)
            : mediaFile.MarkFailed(reason);
        if (transitionResult.IsFailure) return Result<bool>.FromResult(transitionResult);

        return await mediaRepo.UpdateAsync(mediaFile, cancellationToken);
    }
}
