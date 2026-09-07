using Media.Domain.Enums.Properties;
using Media.Domain.Services.Errors;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Media.Unit.Infrastructure.Services;

[TestClass]
public class ImageProcessingServiceTests
{
    private readonly ImageProcessingService _service = new();
    private static readonly MediaFileId AnyMediaFileId = MediaFileId.New();
    private const string AnyBasePath = "listing/owner123";
    private static readonly IReadOnlyList<VariantType> AllImageVariants =
        [VariantType.Thumbnail, VariantType.Medium, VariantType.Large, VariantType.Original];
    private static readonly IReadOnlyList<VariantType> OriginalOnly = [VariantType.Original];

    // ─── StripExifAsync ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task StripExifAsync_WithValidJpeg_ShouldReturnStrippedStream()
    {
        await using var input = await CreateJpegStreamAsync();
        var result = await _service.StripExifAsync(input, AllowedContentTypes.Jpeg, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsTrue(result.Value!.Length > 0);
    }

    [TestMethod]
    public async Task StripExifAsync_WithJpegContainingExif_ShouldRemoveExifProfile()
    {
        await using var input = await CreateJpegWithExifAsync();

        var result = await _service.StripExifAsync(input, AllowedContentTypes.Jpeg, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        using var stripped = await Image.LoadAsync(result.Value!);
        Assert.IsNull(stripped.Metadata.ExifProfile,
            "EXIF profile should be null after stripping");
    }

    [TestMethod]
    public async Task StripExifAsync_WithCorruptImage_ShouldReturnProcessingError()
    {
        var input = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x00, 0x00]);

        var result = await _service.StripExifAsync(input, AllowedContentTypes.Jpeg, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ImageProcessingServiceError.Processing.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task StripExifAsync_WithPdf_ShouldPassthroughUnchanged()
    {
        var content = "%PDF-1.4"u8.ToArray();
        await using var input = new MemoryStream(content);

        var result = await _service.StripExifAsync(input, AllowedContentTypes.Pdf, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        using var reader = new BinaryReader(result.Value!);
        CollectionAssert.AreEqual(content, reader.ReadBytes(content.Length));
    }

    // ─── GenerateVariantsAsync ────────────────────────────────────────────────

    [TestMethod]
    public async Task GenerateVariantsAsync_WithJpegInput_ShouldReturnFourVariants()
    {
        await using var input = await CreateJpegStreamAsync();

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: false, AllImageVariants, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(4, result.Value!.Count);
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_WithJpegInput_ShouldReturnAllFourVariantTypes()
    {
        await using var input = await CreateJpegStreamAsync();

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: false, AllImageVariants, CancellationToken.None);

        var types = result.Value!.Select(v => v.Type).ToHashSet();
        Assert.IsTrue(types.Contains(VariantType.Thumbnail));
        Assert.IsTrue(types.Contains(VariantType.Medium));
        Assert.IsTrue(types.Contains(VariantType.Large));
        Assert.IsTrue(types.Contains(VariantType.Original));
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_WithJpegInput_AllVariantsShouldHaveNonEmptyStreams()
    {
        await using var input = await CreateJpegStreamAsync();

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: false, AllImageVariants, CancellationToken.None);

        foreach (var variant in result.Value!)
        {
            Assert.IsTrue(variant.Data.Length > 0,
                $"Variant {variant.Type} should have a non-empty data stream");
            await variant.Data.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_WithJpegInput_ImageVariantsShouldHaveWebpFilenames()
    {
        await using var input = await CreateJpegStreamAsync();

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: false, AllImageVariants, CancellationToken.None);

        var imageVariants = result.Value!
            .Where(v => v.Type != VariantType.Original)
            .ToList();

        foreach (var variant in imageVariants)
        {
            StringAssert.EndsWith(variant.FileName, ".webp",
                $"Non-original variant {variant.Type} should be stored as .webp");
            await variant.Data.DisposeAsync();
        }

        var original = result.Value!.Single(v => v.Type == VariantType.Original);
        StringAssert.EndsWith(original.FileName, ".jpg");
        await original.Data.DisposeAsync();
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_WithApplyWatermarkFalse_ShouldNotThrowAndReturnFourVariants()
    {
        await using var input = await CreateJpegStreamAsync();

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: false, AllImageVariants, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(4, result.Value!.Count);
        foreach (var v in result.Value!) await v.Data.DisposeAsync();
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_WithWatermarkEnabledButNoEnvVar_ShouldStillReturnFourVariants()
    {
        Environment.SetEnvironmentVariable("MEDIA_WATERMARK_PATH", null);
        await using var input = await CreateJpegStreamAsync();

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: true, AllImageVariants, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(4, result.Value!.Count);
        foreach (var v in result.Value!) await v.Data.DisposeAsync();
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_WithPdfInput_ShouldReturnOnlyOriginalVariant()
    {
        var pdfBytes = "%PDF-1.4"u8.ToArray();
        await using var input = new MemoryStream(pdfBytes);

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Pdf, applyWatermark: false, OriginalOnly, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Value!.Count);
        Assert.AreEqual(VariantType.Original, result.Value![0].Type);
        await result.Value![0].Data.DisposeAsync();
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_WithPdfInput_ShouldNotApplyWatermarkEvenIfRequested()
    {
        var pdfBytes = "%PDF-1.4"u8.ToArray();
        await using var input = new MemoryStream(pdfBytes);

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Pdf, applyWatermark: true, OriginalOnly, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Value!.Count);
        Assert.AreEqual(VariantType.Original, result.Value![0].Type);
        await result.Value![0].Data.DisposeAsync();
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_WithCorruptImage_ShouldReturnProcessingError()
    {
        var input = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x00, 0x00]);

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: false, AllImageVariants, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ImageProcessingServiceError.Processing.Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_ThumbnailShouldHaveDimensions()
    {
        await using var input = await CreateJpegStreamAsync(width: 400, height: 300);

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: false, AllImageVariants, CancellationToken.None);

        var thumbnail = result.Value!.Single(v => v.Type == VariantType.Thumbnail);
        Assert.IsNotNull(thumbnail.Width);
        Assert.IsNotNull(thumbnail.Height);
        Assert.AreEqual(200, thumbnail.Width);
        Assert.AreEqual(200, thumbnail.Height);
        foreach (var v in result.Value!) await v.Data.DisposeAsync();
    }

    [TestMethod]
    public async Task GenerateVariantsAsync_MediumShouldFitWithinBounds()
    {
        await using var input = await CreateJpegStreamAsync(width: 1600, height: 1200);

        var result = await _service.GenerateVariantsAsync(
            input, AnyMediaFileId, AnyBasePath,
            AllowedContentTypes.Jpeg, applyWatermark: false, AllImageVariants, CancellationToken.None);

        var medium = result.Value!.Single(v => v.Type == VariantType.Medium);
        Assert.IsTrue(medium.Width <= 800, $"Medium width {medium.Width} should be ≤ 800");
        Assert.IsTrue(medium.Height <= 600, $"Medium height {medium.Height} should be ≤ 600");
        foreach (var v in result.Value!) await v.Data.DisposeAsync();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<MemoryStream> CreateJpegStreamAsync(int width = 100, int height = 100)
    {
        var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(width, height))
        {
            await image.SaveAsJpegAsync(stream);
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateJpegWithExifAsync()
    {
        var stream = new MemoryStream();
        using var image = new Image<Rgba32>(10, 10);
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Make, "TestCamera");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(40, 1), new Rational(0, 1), new Rational(0, 1)]);
        image.Metadata.ExifProfile = exif;
        await image.SaveAsJpegAsync(stream, new JpegEncoder { SkipMetadata = false });
        stream.Position = 0;
        return stream;
    }
}
