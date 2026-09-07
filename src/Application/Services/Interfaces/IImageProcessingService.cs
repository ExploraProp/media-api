using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using SharedKernel.Kernel.Results;

namespace Media.Application.Services.Interfaces;

public sealed record ProcessedVariant(
    VariantType Type,
    Stream Data,
    string FileName,
    int? Width,
    int? Height,
    long FileSizeBytes);

public interface IImageProcessingService
{
    Task<Result<Stream>> StripExifAsync(Stream input, AllowedContentTypes contentType, CancellationToken ct);

    Task<Result<IReadOnlyList<ProcessedVariant>>> GenerateVariantsAsync(
        Stream promotedFile,
        MediaFileId mediaFileId,
        string baseStoragePath,
        AllowedContentTypes contentType,
        bool applyWatermark,
        IReadOnlyList<VariantType> variantsToGenerate,
        CancellationToken ct);
}
