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
        MediaUpload upload,
        MediaPolicy policy,
        CancellationToken cancellationToken)
    {
        var accessLevel = policy.AccessLevel;
        var isPublic = accessLevel == AccessLevel.Public;
        var promotionPath = MediaStoragePaths.BuildPromotionPath(
            upload.OwnerType,
            upload.OwnerId,
            upload.Id,
            upload.ContentType,
            accessLevel);

        var downloadResult =
            await storageService.DownloadFileAsync(upload.QuarantinePath.Value, cancellationToken);
        if (downloadResult.IsFailure) return Result<MediaPromotionResult>.FromResult(downloadResult);

        await using var stream = downloadResult.Value!;

        var mimeType = upload.ContentType.ToMimeType();
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
                Link.FromDatabase(promotedUrl),
                null,
                MediaFileSize.FromDatabase(stream.Length));
            return Result<MediaPromotionResult>.Success(
                new MediaPromotionResult(promotedUrl, [originalOnly], false));
        }

        stream.Position = 0;

        var baseStoragePath = MediaStoragePaths.BuildBaseStoragePath(
            upload.OwnerType, upload.OwnerId, accessLevel);
        var applyWatermark = policy.WatermarkEnabled && isPublic;

        var variantsResult = await imageProcessingService.GenerateVariantsAsync(
            stream,
            upload.Id,
            baseStoragePath,
            upload.ContentType,
            applyWatermark,
            upload.OwnerType.GetVariantsToGenerate(),
            cancellationToken);
        if (variantsResult.IsFailure) return Result<MediaPromotionResult>.FromResult(variantsResult);

        var domainVariantsResult = await UploadVariantsAsync(
            variantsResult.Value!,
            upload.Id,
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
                    Link.FromDatabase(variantUrl),
                    MediaVariantMetadata.FromDimensions(processed.Width, processed.Height),
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
