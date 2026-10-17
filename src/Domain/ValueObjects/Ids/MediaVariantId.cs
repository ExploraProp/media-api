using SharedKernel.Kernel.Results;

using SharedKernel.Kernel.Domain;

namespace Media.Domain.ValueObjects.Ids;

public sealed record MediaVariantId(Guid Value) : BaseId(Value)
{
    public static MediaVariantId New() => new(Guid.NewGuid());

    public static Result<MediaVariantId> Create(string value)
    {
        var result = BaseId.Create(value, nameof(MediaVariantId));
        return result.IsSuccess
            ? Result<MediaVariantId>.Success(new MediaVariantId(result.Value!))
            : Result<MediaVariantId>.FromResult(result);
    }
}
