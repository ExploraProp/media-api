using SharedKernel.Kernel.Results;

using SharedKernel.Kernel.Domain;

namespace Media.Domain.ValueObjects.Ids;

public sealed record OwnerId(Guid Value) : BaseId(Value)
{
    public static Result<OwnerId> Create(string value)
    {
        var result = BaseId.Create(value, nameof(OwnerId));
        return result.IsSuccess
            ? Result<OwnerId>.Success(new OwnerId(result.Value!))
            : Result<OwnerId>.FromResult(result);
    }
}
