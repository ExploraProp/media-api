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
    IMediaUploadRepo mediaUploadRepo,
    IMediaPolicyRepo mediaPolicyRepo,
    IMediaEventAccessService mediaEventAccessService,
    IMediaEventWaitService mediaEventWaitService)
    : IRequestHandler<WaitMediaFileEventQuery, Result<MediaFileEventWaitDto>>
{
    public async Task<Result<MediaFileEventWaitDto>> Handle(
        WaitMediaFileEventQuery request,
        CancellationToken cancellationToken)
    {
        var readyResult = await mediaRepo.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (readyResult.IsSuccess)
        {
            var mediaFile = readyResult.Value!;
            var accessResult = await mediaEventAccessService.CanAccessEventsAsync(
                mediaFile.OwnerId,
                mediaFile.OwnerType,
                request.RequestingUserId,
                request.RequestingClientUserId,
                cancellationToken);
            if (accessResult.IsFailure)
                return Result<MediaFileEventWaitDto>.FromResult(accessResult);

            var policyResult = await mediaPolicyRepo.GetByOwnerTypeAsync(mediaFile.OwnerType, cancellationToken);
            if (policyResult.IsFailure)
                return Result<MediaFileEventWaitDto>.FromResult(policyResult);

            var accessLevel = policyResult.Value!.AccessLevel;
            var eventVariants = mediaFile.Variants
                .Select(v => new MediaVariantSnapshot(v.VariantType.ToI18nCode(), v.Url.Value))
                .ToList();

            var syntheticApprovedEvent = new MediaFileEvent(
                "fileApproved",
                new FileApproved(
                    mediaFile.ResolvePublicUrl(accessLevel),
                    mediaFile.OwnerId,
                    mediaFile.OwnerType,
                    accessLevel,
                    eventVariants,
                    mediaFile.Id));

            return Result<MediaFileEventWaitDto>.Success(new MediaFileEventWaitDto(syntheticApprovedEvent, false));
        }

        var uploadResult = await mediaUploadRepo.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (uploadResult.IsFailure)
            return Result<MediaFileEventWaitDto>.FromResult(uploadResult);

        var upload = uploadResult.Value!;
        var uploadAccessResult = await mediaEventAccessService.CanAccessEventsAsync(
            upload.OwnerId,
            upload.OwnerType,
            request.RequestingUserId,
            request.RequestingClientUserId,
            cancellationToken);
        if (uploadAccessResult.IsFailure)
            return Result<MediaFileEventWaitDto>.FromResult(uploadAccessResult);

        if (upload.Status is MediaStatus.Rejected or MediaStatus.Failed)
        {
            var syntheticRejectedEvent = new MediaFileEvent(
                "fileRejected",
                new FileRejected(upload.OwnerId, upload.OwnerType, "completed", upload.Id));

            return Result<MediaFileEventWaitDto>.Success(new MediaFileEventWaitDto(syntheticRejectedEvent, false));
        }

        var waitResult = await mediaEventWaitService.WaitForTerminalEventAsync(
            request.MediaFileId,
            cancellationToken);

        return Result<MediaFileEventWaitDto>.Success(waitResult);
    }
}
