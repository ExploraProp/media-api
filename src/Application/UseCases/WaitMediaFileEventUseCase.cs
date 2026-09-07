using Media.Application.Dto;
using Media.Application.Queries;
using Media.Application.Services.Interfaces;
using Media.Domain.Enums.Properties;
using Media.Domain.Events;
using Media.Domain.Repositories;
using Media.Domain.Services.Interfaces;
using MediatR;
using SharedKernel.Kernel.Results;

namespace Media.Application.UseCases;

public sealed class WaitMediaFileEventUseCase(
    IMediaRepo mediaRepo,
    IMediaEventAccessService mediaEventAccessService,
    IMediaEventWaitService mediaEventWaitService)
    : IRequestHandler<WaitMediaFileEventQuery, Result<MediaFileEventWaitDto>>
{
    public async Task<Result<MediaFileEventWaitDto>> Handle(
        WaitMediaFileEventQuery request,
        CancellationToken cancellationToken)
    {
        var mediaFileResult = await mediaRepo.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (mediaFileResult.IsFailure)
            return Result<MediaFileEventWaitDto>.FromResult(mediaFileResult);

        var mediaFile = mediaFileResult.Value!;

        var accessResult = await mediaEventAccessService.CanAccessEventsAsync(
            mediaFile,
            request.RequestingUserId,
            request.RequestingClientUserId,
            cancellationToken);
        if (accessResult.IsFailure)
            return Result<MediaFileEventWaitDto>.FromResult(accessResult);

        // If file is already in a terminal state, return a synthetic event immediately
        if (mediaFile.Status == MediaStatus.Approved)
        {
            var eventVariants = mediaFile.Variants
                .Select(v => new MediaVariantSnapshot(v.VariantType.ToI18nCode(), v.Url.Value))
                .ToList();

            var syntheticApprovedEvent = new MediaFileEvent(
                "fileApproved",
                new FileApproved(
                    mediaFile.PublicUrl?.Value,
                    mediaFile.OwnerId,
                    mediaFile.OwnerType,
                    mediaFile.AccessLevel,
                    eventVariants,
                    mediaFile.Id));

            return Result<MediaFileEventWaitDto>.Success(new MediaFileEventWaitDto(syntheticApprovedEvent, false));
        }

        if (mediaFile.Status is MediaStatus.Rejected or MediaStatus.Failed)
        {
            var syntheticRejectedEvent = new MediaFileEvent(
                "fileRejected",
                new FileRejected(mediaFile.OwnerId, mediaFile.OwnerType, "completed", mediaFile.Id));

            return Result<MediaFileEventWaitDto>.Success(new MediaFileEventWaitDto(syntheticRejectedEvent, false));
        }

        var waitResult = await mediaEventWaitService.WaitForTerminalEventAsync(
            request.MediaFileId,
            cancellationToken);

        return Result<MediaFileEventWaitDto>.Success(waitResult);
    }
}
