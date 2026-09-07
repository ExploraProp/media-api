using Media.Application.Dto;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using MediatR;
using Microsoft.AspNetCore.Http;
using SharedKernel.Kernel.Results;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.Application.Commands;

public sealed record UploadMediaCommand(
    OwnerType OwnerType,
    OwnerId OwnerId,
    MediaFileId MediaFileId,
    MediaType MediaType,
    IFormFile File,
    MediaFileSize MediaFileSize,
    UploadedByClientId UploadedByClientUserId
) : IRequest<Result<MediaFileDto>>
{
    public static Result<UploadMediaCommand> FromDto(UploadMediaDto dto, UploadedByClientId uploadedByClientUserId)
    {
        var ownerTypeResult = OwnerTypeExtensions.TryParseOwnerType(dto.OwnerType);
        var ownerIdResult = OwnerId.Create(dto.OwnerId);
        var mediaFileIdResult = MediaFileId.New();
        var mediaTypeResult = MediaTypeExtensions.TryParseMediaType(dto.MediaType);
        var mediaFileSizeResult = MediaFileSize.Create(dto.File.Length);

        var fieldErrors = new List<Error>();
        if (ownerTypeResult.IsFailure) fieldErrors.AddRange(ownerTypeResult.Errors);
        if (ownerIdResult.IsFailure) fieldErrors.AddRange(ownerIdResult.Errors);
        if (mediaTypeResult.IsFailure) fieldErrors.AddRange(mediaTypeResult.Errors);
        if (mediaFileSizeResult.IsFailure) fieldErrors.AddRange(mediaFileSizeResult.Errors);

        if (fieldErrors.Count > 0)
            return Result<UploadMediaCommand>.Failure(fieldErrors);

        return Result<UploadMediaCommand>.Success(new UploadMediaCommand(
            ownerTypeResult.Value!,
            ownerIdResult.Value!,
            mediaFileIdResult!,
            mediaTypeResult.Value!,
            dto.File,
            mediaFileSizeResult.Value!,
            uploadedByClientUserId
        ));
    }
}
