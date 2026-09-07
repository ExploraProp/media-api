namespace Media.Domain.Services;

/// <summary>Marker type for media actor-gate error-code nameof segments.</summary>
public sealed class MediaAccess
{
    public string Owner { get; private set; } = string.Empty;
}

/// <summary>Marker type for ImageSharp processing error-code nameof segments.</summary>
public sealed class ImageProcessingService
{
    public string Response { get; private set; } = string.Empty;
}

/// <summary>Marker type for Marketing ACL error-code nameof segments.</summary>
public sealed class MarketingService
{
    public string Response { get; private set; } = string.Empty;
}
