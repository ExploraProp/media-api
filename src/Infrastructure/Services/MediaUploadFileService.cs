using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Microsoft.AspNetCore.Http;
using SharedKernel.Kernel.Results;

namespace Media.Infrastructure.Services;

public sealed class MediaUploadFileService(IContentTypeDetectionService contentTypeDetectionService)
    : IMediaUploadFileService
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
}
