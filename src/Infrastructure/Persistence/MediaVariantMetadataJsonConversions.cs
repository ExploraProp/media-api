using Media.Domain.ValueObjects.Properties;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Media.Infrastructure.Persistence;

/// <summary>
/// EF value converters for <see cref="MediaVariantMetadata"/> JSON columns.
/// </summary>
internal static class MediaVariantMetadataJsonConversions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static string? Serialize(MediaVariantMetadata? metadata) =>
        metadata is null ? null : JsonSerializer.Serialize(metadata, Options);

    internal static MediaVariantMetadata? Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<MediaVariantMetadata>(json, Options);
}
