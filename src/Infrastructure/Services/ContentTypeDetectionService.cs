using Media.Application.Services.Interfaces;
using Media.Domain.Enums.Properties;

namespace Media.Infrastructure.Services;

public sealed class ContentTypeDetectionService : IContentTypeDetectionService
{
    public async Task<AllowedContentTypes?> DetectAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[AllowedContentTypesExtensions.RequiredMagicByteHeaderLength];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);

        if (stream.CanSeek)
            stream.Position = 0;

        return AllowedContentTypesExtensions.FromMagicBytes(header.AsSpan(0, read));
    }
}
