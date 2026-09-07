using SharedKernel.Kernel.Results;

using SharedKernel.Kernel.Domain;

namespace Media.Domain.ValueObjects.Ids;

public sealed record AuthId(Guid Value) : BaseId(Value)
{
    public static AuthId New() => new(Guid.NewGuid());

    public static Result<AuthId> Create(string value)
    {
        var result = BaseId.Create(value, nameof(AuthId));
        return result.IsSuccess
            ? Result<AuthId>.Success(new AuthId(result.Value!))
            : Result<AuthId>.FromResult(result);
    }
}
