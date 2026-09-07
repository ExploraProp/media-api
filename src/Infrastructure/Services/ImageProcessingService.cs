using Media.Application.Services.Interfaces;
using Media.Domain.Enums.Properties;
using Media.Domain.Services.Errors;
using Media.Domain.ValueObjects.Ids;
using SharedKernel.Kernel.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Media.Infrastructure.Services;

public sealed class ImageProcessingService : IImageProcessingService
{

    public async Task<Result<Stream>> StripExifAsync(Stream input, AllowedContentTypes contentType, CancellationToken ct)
    {
        if (contentType.IsDocument())
        {
            var passthrough = new MemoryStream();
            await input.CopyToAsync(passthrough, ct);
            passthrough.Position = 0;
            return Result<Stream>.Success(passthrough);
        }

        try
        {
            using var image = await Image.LoadAsync(input, ct);
            image.Metadata.ExifProfile = null;

            var output = new MemoryStream();
            if (contentType == AllowedContentTypes.Jpeg)
            {
                await image.SaveAsJpegAsync(output, new JpegEncoder { SkipMetadata = true }, ct);
            }
            else
            {
                await image.SaveAsync(output, image.Metadata.DecodedImageFormat!, ct);
            }

            output.Position = 0;
            return Result<Stream>.Success(output);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<Stream>.Failure(ImageProcessingServiceError.Processing);
        }
    }

    public async Task<Result<IReadOnlyList<ProcessedVariant>>> GenerateVariantsAsync(
        Stream promotedFile,
        MediaFileId mediaFileId,
        string baseStoragePath,
        AllowedContentTypes contentType,
        bool applyWatermark,
        IReadOnlyList<VariantType> variantsToGenerate,
        CancellationToken ct)
    {
        if (contentType.IsDocument())
        {
            var pdfStream = new MemoryStream();
            await promotedFile.CopyToAsync(pdfStream, ct);
            pdfStream.Position = 0;
            return Result<IReadOnlyList<ProcessedVariant>>.Success(
            [
                new ProcessedVariant(VariantType.Original, pdfStream, VariantType.Original.FileName(contentType), null, null, pdfStream.Length),
            ]);
        }

        try
        {
            var buffer = new MemoryStream();
            await promotedFile.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            using var source = await Image.LoadAsync(buffer, ct);
            Image? watermark = null;

            if (applyWatermark)
            {
                var watermarkPath = Environment.GetEnvironmentVariable("MEDIA_WATERMARK_PATH");
                if (!string.IsNullOrWhiteSpace(watermarkPath) && File.Exists(watermarkPath))
                    watermark = await Image.LoadAsync(watermarkPath, ct);
            }

            try
            {
                var variants = new List<ProcessedVariant>();

                foreach (var variantType in variantsToGenerate)
                {
                    if (variantType == VariantType.Original)
                    {
                        buffer.Position = 0;
                        var originalStream = new MemoryStream();
                        await buffer.CopyToAsync(originalStream, ct);
                        originalStream.Position = 0;
                        variants.Add(new ProcessedVariant(
                            VariantType.Original, originalStream, VariantType.Original.FileName(contentType),
                            source.Width, source.Height, originalStream.Length));
                        continue;
                    }

                    var (width, height) = variantType.GetDimensions();
                    var resizeOptions = new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = variantType.IsCropped() ? ResizeMode.Crop : ResizeMode.Max,
                    };
                    variants.Add(await BuildVariantAsync(source, variantType, resizeOptions, variantType.IsWatermarkable() ? watermark : null, contentType, ct));
                }

                return Result<IReadOnlyList<ProcessedVariant>>.Success(variants);
            }
            finally
            {
                watermark?.Dispose();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<IReadOnlyList<ProcessedVariant>>.Failure(ImageProcessingServiceError.Processing);
        }
    }

    private static async Task<ProcessedVariant> BuildVariantAsync(
        Image source,
        VariantType variantType,
        ResizeOptions resizeOptions,
        Image? watermark,
        AllowedContentTypes contentType,
        CancellationToken ct)
    {
        using var clone = source.Clone(ctx => ctx.Resize(resizeOptions));

        if (watermark != null)
            DrawCenteredWatermark(clone, watermark);

        var output = new MemoryStream();
        await clone.SaveAsWebpAsync(output, new WebpEncoder(), ct);
        output.Position = 0;

        return new ProcessedVariant(
            variantType,
            output,
            variantType.FileName(contentType),
            clone.Width,
            clone.Height,
            output.Length);
    }

    private static void DrawCenteredWatermark(Image canvas, Image watermark)
    {
        var (largeWidth, largeHeight) = VariantType.Large.GetDimensions();
        var scale = Math.Min(
            (float)canvas.Width / largeWidth,
            (float)canvas.Height / largeHeight);

        if (scale < 1f)
        {
            using var scaledWatermark = watermark.Clone(ctx =>
            {
                var width = Math.Max(1, (int)Math.Round(watermark.Width * scale));
                var height = Math.Max(1, (int)Math.Round(watermark.Height * scale));
                ctx.Resize(width, height);
            });
            DrawCentered(canvas, scaledWatermark);
            return;
        }

        DrawCentered(canvas, watermark);
    }

    private static void DrawCentered(Image canvas, Image watermark)
    {
        var x = Math.Max(0, (canvas.Width - watermark.Width) / 2);
        var y = Math.Max(0, (canvas.Height - watermark.Height) / 2);
        canvas.Mutate(ctx => ctx.DrawImage(watermark, new Point(x, y), 1f));
    }
}
