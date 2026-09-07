using Media.Domain.Enums.Errors;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Properties;

public enum MediaType
{
    Photo = 0,
    FloorPlan = 1,
    VirtualTour = 2,
    Document = 3,
}

public static class MediaTypeExtensions
{
    public static string ToI18nCode(this MediaType mediaType) => EnumI18n.GetI18nCode(mediaType);

    public static Result<MediaType> TryParseMediaType(string? value)
    {
        if (Enum.TryParse<MediaType>(value, ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
            return Result<MediaType>.Success(parsed);

        return Result<MediaType>.Failure(MediaTypeError.Invalid);
    }
}
