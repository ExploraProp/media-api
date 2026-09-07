using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using SharedKernel.Kernel.Domain;
using SharedKernel.Kernel.Results;
using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.Entities.Definitions;

public class MediaPolicy : ReferenceEntity<MediaPolicyId>
{
    public OwnerType OwnerType { get; private set; }
    public MediaMaxFileSize MaxFileSizeBytes { get; private set; } = null!;
    public AllowedContentTypes AllowedContentTypes { get; private set; }
    public MediaMaxCount MaxMediaCount { get; private set; } = null!;
    public AccessLevel AccessLevel { get; private set; }
    public bool WatermarkEnabled { get; private set; }

    private MediaPolicy(MediaPolicyId id) : base(id) { }

    [ExcludeFromCodeCoverage]
    private MediaPolicy() : base(new MediaPolicyId(Guid.Empty)) { }

    public static MediaPolicy Create(
        MediaPolicyId id,
        OwnerType ownerType,
        MediaMaxFileSize maxFileSizeBytes,
        AllowedContentTypes allowedContentTypes,
        MediaMaxCount maxMediaCount,
        AccessLevel accessLevel,
        bool watermarkEnabled)
        => new(id)
        {
            OwnerType = ownerType,
            MaxFileSizeBytes = maxFileSizeBytes,
            AllowedContentTypes = allowedContentTypes,
            MaxMediaCount = maxMediaCount,
            AccessLevel = accessLevel,
            WatermarkEnabled = watermarkEnabled,
        };

    public Result<bool> ValidateFileSize(long fileLength) =>
        fileLength > MaxFileSizeBytes.Value
            ? Result<bool>.Failure(MediaPolicyError.TooLarge)
            : Result<bool>.Success(true);

    public Result<AllowedContentTypes> ValidateContentType(AllowedContentTypes? detected) =>
        detected is null || detected.Value.IsNotAllowed(AllowedContentTypes)
            ? Result<AllowedContentTypes>.Failure(MediaPolicyError.UnsupportedContentType)
            : Result<AllowedContentTypes>.Success(detected.Value);

    public Result<bool> ValidateMediaCount(int currentCount) =>
        currentCount >= MaxMediaCount.Value
            ? Result<bool>.Failure(MediaPolicyError.TooMany)
            : Result<bool>.Success(true);
}
