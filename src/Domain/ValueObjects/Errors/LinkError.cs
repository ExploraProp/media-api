using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Results;

namespace Media.Domain.ValueObjects.Errors;

public static class LinkError
{
    public static readonly Error Empty = ValueObjectFailure.Empty(nameof(Link));
    public static readonly Error InvalidFormat = ValueObjectFailure.InvalidFormat(nameof(Link));
}
