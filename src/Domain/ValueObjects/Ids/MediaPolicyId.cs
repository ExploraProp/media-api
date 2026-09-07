using SharedKernel.Kernel.Results;

using SharedKernel.Kernel.Domain;

namespace Media.Domain.ValueObjects.Ids;

public sealed record MediaPolicyId(Guid Value) : BaseId(Value)
{
    public static Result<MediaPolicyId> Create(string value)
    {
        var result = BaseId.Create(value, nameof(MediaPolicyId));
        return result.IsSuccess
            ? Result<MediaPolicyId>.Success(new MediaPolicyId(result.Value!))
            : Result<MediaPolicyId>.FromResult(result);
    }
}
