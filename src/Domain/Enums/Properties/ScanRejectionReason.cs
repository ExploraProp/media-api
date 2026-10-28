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
    public static string ToI18nCode(this ScanRejectionReason reason) => EnumI18n.GetI18nCode(reason);

    public static Result<ScanRejectionReason> TryParseScanRejectionReason(string? value) =>
        EnumParse.TryParse<ScanRejectionReason>(value, ScanRejectionReasonError.Invalid, ScanRejectionReasonError.Empty);

    public static bool TryFromOrdinalScanRejectionReason(string? value, out ScanRejectionReason result) =>
        EnumParse.TryFromOrdinal(value, out result);
}
