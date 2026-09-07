using Media.Domain.Enums.Errors;
using SharedKernel.Kernel.Results;

namespace Media.Domain.Enums.Properties;

public enum OwnerType
{
    User = 0,
    Listing = 1,
    Identification = 2,
    VerificationSubmission = 3,
}

public static class OwnerTypeExtensions
{
    public static IReadOnlyList<VariantType> GetVariantsToGenerate(this OwnerType ownerType) =>
        ownerType switch
        {
            OwnerType.Listing => [VariantType.Thumbnail, VariantType.Medium, VariantType.Large, VariantType.Original],
            OwnerType.User => [VariantType.Thumbnail, VariantType.Medium, VariantType.Original],
            OwnerType.Identification => [VariantType.Original],
            OwnerType.VerificationSubmission => [VariantType.Original],
            _ => [VariantType.Original],
        };

    public static string ToI18nCode(this OwnerType ownerType) => EnumI18n.GetI18nCode(ownerType);

    public static Result<OwnerType> TryParseOwnerType(string? value)
    {
        if (Enum.TryParse<OwnerType>(value, ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
            return Result<OwnerType>.Success(parsed);

        return Result<OwnerType>.Failure(OwnerTypeError.Invalid);
    }
}
