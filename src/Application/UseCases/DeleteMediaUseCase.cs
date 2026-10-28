using Media.Application.Commands;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Application.UseCases;

public sealed class DeleteMediaUseCase(
    IMediaRepo mediaRepo,
    IMediaUploadRepo mediaUploadRepo,
    IStorageService storageService,
    ILogger<DeleteMediaUseCase> logger) : IRequestHandler<DeleteMediaCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteMediaCommand command, CancellationToken cancellationToken)
    {
        var readyResult = await mediaRepo.GetByIdAsync(command.MediaFileId, cancellationToken);
        if (readyResult.IsSuccess)
            return await DeleteReadyAsync(readyResult.Value!, command, cancellationToken);

        var uploadResult = await mediaUploadRepo.GetByIdAsync(command.MediaFileId, cancellationToken);
        if (uploadResult.IsFailure)
            return Result<bool>.FromResult(uploadResult);

        return await DeleteUploadAsync(uploadResult.Value!, command, cancellationToken);
    }

    private async Task<Result<bool>> DeleteReadyAsync(
        Domain.Entities.Definitions.MediaFile mediaFile,
        DeleteMediaCommand command,
        CancellationToken cancellationToken)
    {
        var isOwner = command.RequestingUserId is not null
            && mediaFile.UploadedByClientUserId.Value == command.RequestingUserId.Value;
        if (!isOwner && !command.IsAdmin)
            return Result<bool>.Failure(MediaFileError.Permission);

        var deleteReadyResult = mediaFile.Delete();
        if (deleteReadyResult.IsFailure) return Result<bool>.FromResult(deleteReadyResult);

        foreach (var variant in mediaFile.Variants)
        {
            try
            {
                if (variant.Url.Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    await storageService.DeleteFileByUrlAsync(variant.Url.Value);
                else
                    await storageService.DeleteFileAsync(variant.Url.Value);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete variant blob {Url}; continuing", variant.Url.Value);
            }
        }

        return await mediaRepo.DeleteAsync(command.MediaFileId, cancellationToken);
    }

    private async Task<Result<bool>> DeleteUploadAsync(
        Domain.Entities.Definitions.MediaUpload upload,
        DeleteMediaCommand command,
        CancellationToken cancellationToken)
    {
        var isOwner = command.RequestingUserId is not null
            && upload.UploadedByClientUserId.Value == command.RequestingUserId.Value;
        if (!isOwner && !command.IsAdmin)
            return Result<bool>.Failure(MediaFileError.Permission);

        var deleteReadyResult = upload.Delete();
        if (deleteReadyResult.IsFailure) return Result<bool>.FromResult(deleteReadyResult);

        if (upload.Status is MediaStatus.Pending or MediaStatus.Rejected or MediaStatus.Failed)
        {
            try
            {
                await storageService.DeleteFileAsync(upload.QuarantinePath.Value);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete quarantine blob {Path}; continuing", upload.QuarantinePath.Value);
            }
        }

        return await mediaUploadRepo.DeleteAsync(command.MediaFileId, cancellationToken);
    }
}
