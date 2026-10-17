using Media.Domain.Enums.Properties;

namespace Media.Unit.Domain.Enums.Properties;

[TestClass]
public class AllowedContentTypesExtensionsTests
{
    [DataTestMethod]
    [DataRow("FFD8FFE0", AllowedContentTypes.Jpeg)]
    [DataRow("89504E470D0A", AllowedContentTypes.Png)]
    [DataRow("474946383961", AllowedContentTypes.Gif)]
    [DataRow("524946460000000057454250", AllowedContentTypes.WebP)]
    [DataRow("255044462D", AllowedContentTypes.Pdf)]
    public void FromMagicBytes_KnownHeader_ReturnsType(string hexHeader, AllowedContentTypes expected)
    {
        ReadOnlySpan<byte> header = Convert.FromHexString(hexHeader);

        Assert.AreEqual(expected, AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [DataTestMethod]
    [DataRow("4D5A9000")]
    [DataRow("FFD8")]
    public void FromMagicBytes_UnrecognizedOrTooShort_ReturnsNull(string hexHeader)
    {
        ReadOnlySpan<byte> header = Convert.FromHexString(hexHeader);

        Assert.IsNull(AllowedContentTypesExtensions.FromMagicBytes(header));
    }

    [DataTestMethod]
    [DataRow(AllowedContentTypes.Jpeg, AllowedContentTypes.Jpeg | AllowedContentTypes.Png, true)]
    [DataRow(AllowedContentTypes.Pdf, AllowedContentTypes.Jpeg | AllowedContentTypes.Png, false)]
    [DataRow(AllowedContentTypes.None, AllowedContentTypes.Jpeg, false)]
    public void IsAllowed_ReturnsExpected(
        AllowedContentTypes detected, AllowedContentTypes policy, bool expected)
    {
        Assert.AreEqual(expected, detected.IsAllowed(policy));
    }

    [DataTestMethod]
    [DataRow(AllowedContentTypes.Gif, AllowedContentTypes.Jpeg, true)]
    [DataRow(AllowedContentTypes.Jpeg, AllowedContentTypes.Jpeg, false)]
    public void IsNotAllowed_NegatesIsAllowed(
        AllowedContentTypes detected, AllowedContentTypes policy, bool expected)
    {
        Assert.AreEqual(expected, detected.IsNotAllowed(policy));
    }

    [DataTestMethod]
    [DataRow(AllowedContentTypes.Jpeg, "image/jpeg")]
    [DataRow(AllowedContentTypes.Png, "image/png")]
    [DataRow(AllowedContentTypes.Gif, "image/gif")]
    [DataRow(AllowedContentTypes.WebP, "image/webp")]
    [DataRow(AllowedContentTypes.Pdf, "application/pdf")]
    [DataRow(AllowedContentTypes.None, "application/octet-stream")]
    public void ToMimeType_MapsKnownTypes(AllowedContentTypes contentType, string expected)
    {
        Assert.AreEqual(expected, contentType.ToMimeType());
    }

    [DataTestMethod]
    [DataRow(AllowedContentTypes.Jpeg, ".jpg")]
    [DataRow(AllowedContentTypes.Png, ".png")]
    [DataRow(AllowedContentTypes.Gif, ".gif")]
    [DataRow(AllowedContentTypes.WebP, ".webp")]
    [DataRow(AllowedContentTypes.Pdf, ".pdf")]
    [DataRow(AllowedContentTypes.None, "")]
    public void ToFileExtension_MapsKnownTypes(AllowedContentTypes contentType, string expected)
    {
        Assert.AreEqual(expected, contentType.ToFileExtension());
    }
}
