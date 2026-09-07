using Media.Application.Commands;
using Media.Application.Dto;
using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using MediatR;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;
using System.Threading.Channels;

namespace Media.Application.UseCases;

public sealed class UploadMediaUseCase(
    IMediaPolicyRepo mediaPolicyRepo,
    IMediaRepo mediaRepo,
    IStorageService storageService,
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

        var countResult = await mediaRepo.CountByOwnerAsync(command.OwnerId.Value, command.OwnerType, cancellationToken);
        if (countResult.IsFailure) return Result<MediaFileDto>.FromResult(countResult);

        var mediaCountResult = policy.ValidateMediaCount(countResult.Value!);
        if (mediaCountResult.IsFailure) return Result<MediaFileDto>.FromResult(mediaCountResult);

        var stripResult = await imageProcessingService.StripExifAsync(prepared.Stream, prepared.ContentType, cancellationToken);
        if (stripResult.IsFailure) return Result<MediaFileDto>.FromResult(stripResult);

        await using var strippedStream = stripResult.Value!;
        var uploadResult = await storageService.UploadToQuarantineAsync(
            strippedStream,
            command.File.FileName,
            command.OwnerType.ToI18nCode(),
            command.OwnerId.Value,
            command.MediaFileId.Value,
            prepared.ContentType.ToMimeType(),
            cancellationToken);

        if (uploadResult.IsFailure) return Result<MediaFileDto>.FromResult(uploadResult);

        var quarantinePathResult = StoragePath.FromQuarantineUpload(uploadResult.Value);
        if (quarantinePathResult.IsFailure) return Result<MediaFileDto>.FromResult(quarantinePathResult);
        var quarantinePath = quarantinePathResult.Value!;

        var mediaFileResult = MediaFile.Create(
            command.MediaFileId,
            command.OwnerId,
            command.OwnerType,
            command.MediaType,
            MediaStatus.Pending,
            quarantinePath,
            prepared.ContentType,
            command.MediaFileSize,
            command.UploadedByClientUserId);
        if (mediaFileResult.IsFailure) return Result<MediaFileDto>.FromResult(mediaFileResult);
        var mediaFile = mediaFileResult.Value!;

        var addResult = await mediaRepo.AddAsync(mediaFile, cancellationToken);
        if (addResult.IsFailure) return Result<MediaFileDto>.FromResult(addResult);

        await _channel.Writer.WriteAsync(mediaFile.Id, cancellationToken);

        return Result<MediaFileDto>.Success(MediaFileDto.FromEntity(mediaFile, catalogLabels));
    }
}
