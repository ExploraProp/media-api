using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.ValueObjects.Errors;

public static class StoragePathError
{
    public static readonly Error Empty = ValueObjectFailure.Empty(nameof(StoragePath));
    public static readonly Error TooLong = ValueObjectFailure.TooLong(nameof(StoragePath), StoragePath.MaxLength);
    public static readonly Error InvalidFormat = ValueObjectFailure.InvalidFormat(nameof(StoragePath));
}
