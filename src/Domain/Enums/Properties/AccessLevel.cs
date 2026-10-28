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

    public static Result<AccessLevel> TryParseAccessLevel(string? value) =>
        EnumParse.TryParse<AccessLevel>(value, AccessLevelError.Invalid, AccessLevelError.Empty);

    public static bool TryFromOrdinalAccessLevel(string? value, out AccessLevel result) =>
        EnumParse.TryFromOrdinal(value, out result);
}
