using Media.Domain.Enums.Errors;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Properties;

public enum AccessLevel
{
    Public = 0,
    Private = 1,
}

public static class AccessLevelExtensions
{
    public static string ToI18nCode(this AccessLevel level) => EnumI18n.GetI18nCode(level);

    public static Result<AccessLevel> TryParseAccessLevel(string? value)
    {
        if (Enum.TryParse<AccessLevel>(value, ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
            return Result<AccessLevel>.Success(parsed);

        return Result<AccessLevel>.Failure(AccessLevelError.Invalid);
    }
}
