using SharedKernel.Kernel.Results;

using SharedKernel.Kernel.Domain;

namespace Media.Domain.ValueObjects.Ids;

public sealed record UploadedByClientId(Guid Value) : BaseId(Value)
{
    public static UploadedByClientId New() => new(Guid.NewGuid());

    public static Result<UploadedByClientId> Create(string value)
    {
        var result = BaseId.Create(value, nameof(UploadedByClientId));
        return result.IsSuccess
            ? Result<UploadedByClientId>.Success(new UploadedByClientId(result.Value!))
            : Result<UploadedByClientId>.FromResult(result);
    }
}
