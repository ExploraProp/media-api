using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Microsoft.AspNetCore.Http;
using SharedKernel.Kernel.Results;

namespace Media.Application.Services.Interfaces;

public interface IMediaUploadFileService
{
    Task<Result<PreparedMediaUploadFile>> PrepareAsync(IFormFile file, MediaPolicy policy, CancellationToken ct);
}

public sealed class PreparedMediaUploadFile(Stream stream, AllowedContentTypes contentType) : IAsyncDisposable
{
    public Stream Stream { get; } = stream;

    public AllowedContentTypes ContentType { get; } = contentType;

    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}
