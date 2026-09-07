using Media.Domain.Enums.Errors;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Properties;

public enum ScanRejectionReason
{
    Infected = 1,
    ScanFailed = 2,
}

public static class ScanRejectionReasonExtensions
{
    public static string ToReasonCode(this ScanRejectionReason reason) => reason switch
    {
        ScanRejectionReason.Infected => "infected",
        ScanRejectionReason.ScanFailed => "scanFailed",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };

    public static string ToI18nCode(this ScanRejectionReason reason) => EnumI18n.GetI18nCode(reason);

    public static Result<ScanRejectionReason> TryParseScanRejectionReason(string? value)
    {
        if (Enum.TryParse<ScanRejectionReason>(value, ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
            return Result<ScanRejectionReason>.Success(parsed);

        return Result<ScanRejectionReason>.Failure(ScanRejectionReasonError.Invalid);
    }
}
