using Media.Domain.ValueObjects.Ids;

using SharedKernel.Kernel.Domain;

namespace Media.Unit.Domain.ValueObjects.Ids;

[TestClass]
public class MediaPolicyIdTests : BaseIdTestBase<MediaPolicyId>
{
    protected override string IdTypeName => nameof(MediaPolicyId);

    protected override Result<MediaPolicyId> CreateId(string value)
    {
        var result = SharedKernel.Kernel.Domain.BaseId.Create(value, nameof(MediaPolicyId));
        return result.IsSuccess
            ? Result<MediaPolicyId>.Success(new MediaPolicyId(result.Value!))
            : Result<MediaPolicyId>.FromResult(result);
    }

    protected override Guid GetGuidValue(MediaPolicyId id) => id.Value;
}
