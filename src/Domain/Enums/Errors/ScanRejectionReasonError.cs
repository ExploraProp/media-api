using Media.Domain.Enums.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Errors;

public static class ScanRejectionReasonError
{
    public static readonly Error Invalid = ValueObjectFailure.InvalidFormat(nameof(ScanRejectionReason));
    public static readonly Error Empty = ValueObjectFailure.Empty(nameof(ScanRejectionReason));
}
