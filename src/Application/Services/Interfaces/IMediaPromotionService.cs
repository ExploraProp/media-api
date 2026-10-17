using Media.Domain.Entities.Definitions;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Application.Services.Interfaces;

public sealed record MediaPromotionResult(
    string? PromotedUrl,
    IReadOnlyList<PromotedVariantInput> Variants,
    bool IsPublic);

public interface IMediaPromotionService
{
    Task<Result<MediaPromotionResult>> PromoteAsync(
        MediaUpload upload,
        MediaPolicy policy,
        CancellationToken cancellationToken);
}
