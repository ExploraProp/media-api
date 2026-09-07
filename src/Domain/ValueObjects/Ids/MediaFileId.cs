using SharedKernel.Kernel.Results;

using SharedKernel.Kernel.Domain;

namespace Media.Domain.ValueObjects.Ids;

public sealed record MediaFileId(Guid Value) : BaseId(Value)
{
    public static MediaFileId New() => new(Guid.NewGuid());

    public static Result<MediaFileId> Create(string value)
    {
        var result = BaseId.Create(value, nameof(MediaFileId));
        return result.IsSuccess
            ? Result<MediaFileId>.Success(new MediaFileId(result.Value!))
            : Result<MediaFileId>.FromResult(result);
    }
}
