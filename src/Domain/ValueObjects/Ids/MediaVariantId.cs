using SharedKernel.Kernel.Domain;
namespace Media.Domain.ValueObjects.Ids;

public sealed record MediaVariantId(Guid Value) : BaseId(Value)
{
    public static MediaVariantId New() => new(Guid.NewGuid());
}
