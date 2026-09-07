using Media.Domain.Enums.Properties;

namespace Media.Unit.Domain.Enums.Properties;

[TestClass]
public class AllowedContentTypesExtensionsTests
{
    [TestMethod]
    public void FromMagicBytes_WithJpegHeader_ShouldReturnJpeg()
    {
        ReadOnlySpan<byte> header = [0xFF, 0xD8, 0xFF, 0xE0];

        Assert.AreEqual(AllowedContentTypes.Jpeg, AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [TestMethod]
    public void FromMagicBytes_WithPngHeader_ShouldReturnPng()
    {
        ReadOnlySpan<byte> header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A];

        Assert.AreEqual(AllowedContentTypes.Png, AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [TestMethod]
    public void FromMagicBytes_WithGifHeader_ShouldReturnGif()
    {
        ReadOnlySpan<byte> header = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];

        Assert.AreEqual(AllowedContentTypes.Gif, AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [TestMethod]
    public void FromMagicBytes_WithWebPHeader_ShouldReturnWebP()
    {
        ReadOnlySpan<byte> header =
        [
            0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50,
        ];

        Assert.AreEqual(AllowedContentTypes.WebP, AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [TestMethod]
    public void FromMagicBytes_WithPdfHeader_ShouldReturnPdf()
    {
        ReadOnlySpan<byte> header = [0x25, 0x50, 0x44, 0x46, 0x2D];

        Assert.AreEqual(AllowedContentTypes.Pdf, AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [TestMethod]
    public void FromMagicBytes_WithUnknownHeader_ShouldReturnNull()
    {
        ReadOnlySpan<byte> header = [0x4D, 0x5A, 0x90, 0x00];

        Assert.IsNull(AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [TestMethod]
    public void FromMagicBytes_WithTooShortHeader_ShouldReturnNull()
    {
        ReadOnlySpan<byte> header = [0xFF, 0xD8];

        Assert.IsNull(AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [TestMethod]
    public void IsAllowed_WhenDetectedTypeIsInPolicy_ShouldReturnTrue()
    {
        Assert.IsTrue(AllowedContentTypes.Jpeg.IsAllowed(AllowedContentTypes.Jpeg | AllowedContentTypes.Png));
    }

    [TestMethod]
    public void IsAllowed_WhenDetectedTypeIsNotInPolicy_ShouldReturnFalse()
    {
        Assert.IsFalse(AllowedContentTypes.Pdf.IsAllowed(AllowedContentTypes.Jpeg | AllowedContentTypes.Png));
    }

    [TestMethod]
    public void IsAllowed_WhenDetectedTypeIsNone_ShouldReturnFalse()
    {
        Assert.IsFalse(AllowedContentTypes.None.IsAllowed(AllowedContentTypes.Jpeg));
    }

    [TestMethod]
    public void IsNotAllowed_ShouldNegateIsAllowed()
    {
        Assert.IsTrue(AllowedContentTypes.Gif.IsNotAllowed(AllowedContentTypes.Jpeg));
        Assert.IsFalse(AllowedContentTypes.Jpeg.IsNotAllowed(AllowedContentTypes.Jpeg));
    }

    [TestMethod]
    public void ToMimeType_ShouldMapKnownTypes()
    {
        Assert.AreEqual("image/jpeg", AllowedContentTypes.Jpeg.ToMimeType());
        Assert.AreEqual("image/png", AllowedContentTypes.Png.ToMimeType());
        Assert.AreEqual("image/gif", AllowedContentTypes.Gif.ToMimeType());
        Assert.AreEqual("image/webp", AllowedContentTypes.WebP.ToMimeType());
        Assert.AreEqual("application/pdf", AllowedContentTypes.Pdf.ToMimeType());
    }

    [TestMethod]
    public void ToMimeType_WithNone_ShouldReturnOctetStream()
    {
        Assert.AreEqual("application/octet-stream", AllowedContentTypes.None.ToMimeType());
    }

    [TestMethod]
    public void ToFileExtension_ShouldMapKnownTypes()
    {
        Assert.AreEqual(".jpg", AllowedContentTypes.Jpeg.ToFileExtension());
        Assert.AreEqual(".png", AllowedContentTypes.Png.ToFileExtension());
        Assert.AreEqual(".gif", AllowedContentTypes.Gif.ToFileExtension());
        Assert.AreEqual(".webp", AllowedContentTypes.WebP.ToFileExtension());
        Assert.AreEqual(".pdf", AllowedContentTypes.Pdf.ToFileExtension());
    }

    [TestMethod]
    public void ToFileExtension_WithNone_ShouldReturnEmpty()
    {
        Assert.AreEqual(string.Empty, AllowedContentTypes.None.ToFileExtension());
    }
}
