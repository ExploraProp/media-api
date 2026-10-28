using Media.Application.Commands;
using Media.Application.Dto;
using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;
using System.Threading.Channels;

namespace Media.Application.UseCases;

public sealed class UploadMediaUseCase(
    IMediaPolicyRepo mediaPolicyRepo,
    IMediaRepo mediaRepo,
    IMediaUploadRepo mediaUploadRepo,
    IMediaUploadFileService mediaUploadFileService,
    IImageProcessingService imageProcessingService,
    Channel<MediaFileId> channel,
    ICatalogLabelLocalizer catalogLabels) : IRequestHandler<UploadMediaCommand, Result<MediaFileDto>>
{
    private readonly Channel<MediaFileId> _channel = channel;

    public async Task<Result<MediaFileDto>> Handle(UploadMediaCommand command, CancellationToken cancellationToken)
    {
        var policyResult = await mediaPolicyRepo.GetByOwnerTypeAsync(command.OwnerType, cancellationToken);
        if (policyResult.IsFailure) return Result<MediaFileDto>.FromResult(policyResult);
        var policy = policyResult.Value!;

        var preparedResult = await mediaUploadFileService.PrepareAsync(command.File, policy, cancellationToken);
        if (preparedResult.IsFailure) return Result<MediaFileDto>.FromResult(preparedResult);
        await using var prepared = preparedResult.Value!;

        var readyCountResult = await mediaRepo.CountByOwnerAsync(command.OwnerId.Value, command.OwnerType, cancellationToken);
        if (readyCountResult.IsFailure) return Result<MediaFileDto>.FromResult(readyCountResult);

        var inFlightCountResult = await mediaUploadRepo.CountInFlightByOwnerAsync(
            command.OwnerId.Value, command.OwnerType, cancellationToken);
        if (inFlightCountResult.IsFailure) return Result<MediaFileDto>.FromResult(inFlightCountResult);

        var mediaCountResult = policy.ValidateMediaCount(readyCountResult.Value! + inFlightCountResult.Value!);
        if (mediaCountResult.IsFailure) return Result<MediaFileDto>.FromResult(mediaCountResult);

        var stripResult = await imageProcessingService.StripExifAsync(prepared.Stream, prepared.ContentType, cancellationToken);
        if (stripResult.IsFailure) return Result<MediaFileDto>.FromResult(stripResult);

        await using var strippedStream = stripResult.Value!;
        var quarantinePathResult = await mediaUploadFileService.UploadToQuarantineAsync(
            strippedStream,
            command.File.FileName,
            command.OwnerType,
            command.OwnerId.Value,
            command.MediaFileId.Value,
            prepared.ContentType,
            cancellationToken);
        if (quarantinePathResult.IsFailure) return Result<MediaFileDto>.FromResult(quarantinePathResult);
        var quarantinePath = quarantinePathResult.Value!;

        var uploadResult = MediaUpload.Create(
            command.MediaFileId,
            command.OwnerId,
            command.OwnerType,
            quarantinePath,
            prepared.ContentType,
            command.MediaFileSize,
            command.UploadedByClientUserId);
        if (uploadResult.IsFailure) return Result<MediaFileDto>.FromResult(uploadResult);
        var upload = uploadResult.Value!;

        var addResult = await mediaUploadRepo.AddAsync(upload, cancellationToken);
        if (addResult.IsFailure) return Result<MediaFileDto>.FromResult(addResult);

        await _channel.Writer.WriteAsync(upload.Id, cancellationToken);

        return Result<MediaFileDto>.Success(MediaFileDto.FromEntity(upload, catalogLabels));
    }
}
