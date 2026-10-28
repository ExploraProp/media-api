using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Properties;
using Microsoft.AspNetCore.Http;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Infrastructure.Services;

public sealed class MediaUploadFileService(
    IContentTypeDetectionService contentTypeDetectionService,
    IStorageService storageService) : IMediaUploadFileService
{
    public async Task<Result<PreparedMediaUploadFile>> PrepareAsync(
        IFormFile file,
        MediaPolicy policy,
        CancellationToken ct)
    {
        var sizeResult = policy.ValidateFileSize(file.Length);
        if (sizeResult.IsFailure)
            return Result<PreparedMediaUploadFile>.FromResult(sizeResult);

        Stream? stream = file.OpenReadStream();
        try
        {
            var detected = await contentTypeDetectionService.DetectAsync(stream, ct);
            var typeResult = policy.ValidateContentType(detected);
            if (typeResult.IsFailure)
                return Result<PreparedMediaUploadFile>.FromResult(typeResult);

            var prepared = new PreparedMediaUploadFile(stream, typeResult.Value!);
            stream = null;
            return Result<PreparedMediaUploadFile>.Success(prepared);
        }
        finally
        {
            if (stream is not null)
                await stream.DisposeAsync();
        }
    }

    public async Task<Result<StoragePath>> UploadToQuarantineAsync(
        Stream fileStream,
        string fileName,
        OwnerType ownerType,
        Guid ownerId,
        Guid mediaFileId,
        AllowedContentTypes contentType,
        CancellationToken cancellationToken = default)
    {
        var uploadResult = await storageService.UploadToQuarantineAsync(
            fileStream,
            fileName,
            ownerType.ToI18nCode(),
            ownerId,
            mediaFileId,
            contentType.ToMimeType(),
            cancellationToken);
        if (uploadResult.IsFailure)
            return Result<StoragePath>.FromResult(uploadResult);

        return StoragePath.Create(uploadResult.Value);
    }
}
