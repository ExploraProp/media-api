using Media.Domain.Enums.Properties;

namespace Media.Application.Services.Interfaces;

public interface IContentTypeDetectionService
{
    Task<AllowedContentTypes?> DetectAsync(Stream stream, CancellationToken ct);
}
