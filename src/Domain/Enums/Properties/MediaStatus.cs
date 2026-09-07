using Media.Domain.Enums.Errors;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Properties;

public enum MediaStatus
{
    Pending = 0,
    Scanning = 1,
    Approved = 2,
    Rejected = 3,
    Failed = 4,
}

public static class MediaStatusExtensions
{
    public static string ToI18nCode(this MediaStatus status) => EnumI18n.GetI18nCode(status);

    public static Result<MediaStatus> TryParseMediaStatus(string? value)
    {
        if (Enum.TryParse<MediaStatus>(value, ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
            return Result<MediaStatus>.Success(parsed);

        return Result<MediaStatus>.Failure(MediaStatusError.Invalid);
    }
}
