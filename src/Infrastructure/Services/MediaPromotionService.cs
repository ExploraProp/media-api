using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Services;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Infrastructure.Services;

public sealed class MediaPromotionService(
    IStorageService storageService,
    IImageProcessingService imageProcessingService) : IMediaPromotionService
{
    public async Task<Result<MediaPromotionResult>> PromoteAsync(
        MediaFile mediaFile,
        MediaPolicy policy,
        CancellationToken cancellationToken)
    {
        var isPublic = policy.AccessLevel == AccessLevel.Public;
        var promotionPath = MediaStoragePaths.BuildPromotionPath(
            mediaFile.OwnerType,
            mediaFile.OwnerId,
            mediaFile.Id,
            mediaFile.ContentType,
            policy.AccessLevel);

        var downloadResult = 
            await storageService.DownloadFileAsync(mediaFile.QuarantinePath.Value, cancellationToken);
        if (downloadResult.IsFailure) return Result<MediaPromotionResult>.FromResult(downloadResult);

        await using var stream = downloadResult.Value!;

        var mimeType = mediaFile.ContentType.ToMimeType();
        var uploadResult = await storageService.UploadStreamToPathAsync(
            stream, promotionPath, mimeType, isPublic, cancellationToken);
        if (uploadResult.IsFailure) return Result<MediaPromotionResult>.FromResult(uploadResult);
        var promotedUrl = uploadResult.Value!;

        // Private files: skip variant generation — reviewers access the file via signed URL on the original.
        // Generating thumbnail/medium/large for files that are rarely accessed wastes storage.
        if (!isPublic)
        {
            var originalOnly = new PromotedVariantInput(
                VariantType.Original,
                VariantUrl.FromStorage(promotedUrl),
                null,
                null,
                MediaFileSize.FromDatabase(stream.Length));
            return Result<MediaPromotionResult>.Success(
                new MediaPromotionResult(promotedUrl, [originalOnly], false));
        }

        stream.Position = 0;

        var baseStoragePath = MediaStoragePaths.BuildBaseStoragePath(
            mediaFile.OwnerType, mediaFile.OwnerId, policy.AccessLevel);
        var applyWatermark = policy.WatermarkEnabled && isPublic;

        var variantsResult = await imageProcessingService.GenerateVariantsAsync(
            stream,
            mediaFile.Id,
            baseStoragePath,
            mediaFile.ContentType,
            applyWatermark,
            mediaFile.OwnerType.GetVariantsToGenerate(),
            cancellationToken);
        if (variantsResult.IsFailure) return Result<MediaPromotionResult>.FromResult(variantsResult);

        var domainVariantsResult = await UploadVariantsAsync(
            variantsResult.Value!,
            mediaFile.Id,
            baseStoragePath,
            promotedUrl,
            isPublic,
            cancellationToken);
        if (domainVariantsResult.IsFailure) return Result<MediaPromotionResult>.FromResult(domainVariantsResult);

        return Result<MediaPromotionResult>.Success(
            new MediaPromotionResult(promotedUrl, domainVariantsResult.Value!, isPublic));
    }

    private async Task<Result<IReadOnlyList<PromotedVariantInput>>> UploadVariantsAsync(
        IReadOnlyList<ProcessedVariant> processedVariants,
        MediaFileId mediaFileId,
        string baseStoragePath,
        string promotedUrl,
        bool isPublic,
        CancellationToken cancellationToken)
    {
        var uploadedUrls = new List<string>();
        var promotedVariants = new List<PromotedVariantInput>();

        try
        {
            foreach (var processed in processedVariants)
            {
                await using var _ = processed.Data;

                string variantUrl;
                if (processed.Type == VariantType.Original)
                {
                    variantUrl = promotedUrl;
                }
                else
                {
                    var storagePath = MediaStoragePaths.BuildVariantPath(
                        baseStoragePath, mediaFileId, processed.FileName);
                    var variantUploadResult = await storageService.UploadStreamToPathAsync(
                        processed.Data,
                        storagePath,
                        "image/webp",
                        isPublic,
                        cancellationToken);

                    if (variantUploadResult.IsFailure)
                    {
                        await CleanupUploadedVariantsAsync(uploadedUrls);
                        return Result<IReadOnlyList<PromotedVariantInput>>.FromResult(variantUploadResult);
                    }

                    variantUrl = variantUploadResult.Value!;
                    uploadedUrls.Add(variantUrl);
                }

                promotedVariants.Add(new PromotedVariantInput(
                    processed.Type,
                    VariantUrl.FromStorage(variantUrl),
                    processed.Width.HasValue ? VariantDimension.From(processed.Width.Value) : null,
                    processed.Height.HasValue ? VariantDimension.From(processed.Height.Value) : null,
                    MediaFileSize.FromDatabase(processed.FileSizeBytes)));
            }
        }
        catch (Exception)
        {
            await CleanupUploadedVariantsAsync(uploadedUrls);
            throw;
        }

        return Result<IReadOnlyList<PromotedVariantInput>>.Success(promotedVariants);
    }

    private async Task CleanupUploadedVariantsAsync(List<string> uploadedUrls)
    {
        foreach (var url in uploadedUrls)
        {
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                await storageService.DeleteFileByUrlAsync(url);
            else
                await storageService.DeleteFileAsync(url);
        }
    }
}
