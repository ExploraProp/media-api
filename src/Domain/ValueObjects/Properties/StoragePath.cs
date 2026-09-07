using Media.Domain.ValueObjects.Errors;
using SharedKernel.Kernel.Results;
using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.ValueObjects.Properties;

public sealed record StoragePath
{
    public const int MaxLength = 512;
    public string Value { get; }

    [ExcludeFromCodeCoverage]
    private StoragePath() => Value = string.Empty;

    private StoragePath(string value) => Value = value;

    public static Result<StoragePath> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<StoragePath>.Failure(StoragePathError.Empty);

        if (value.Length > MaxLength)
            return Result<StoragePath>.Failure(StoragePathError.TooLong);

        if (value.Contains("..", StringComparison.Ordinal))
            return Result<StoragePath>.Failure(StoragePathError.InvalidFormat);

        return Result<StoragePath>.Success(new StoragePath(value));
    }

    /// <summary>Parses a path produced by the quarantine upload pipeline.</summary>
    public static Result<StoragePath> FromQuarantineUpload(string? value) => Create(value);

    public static StoragePath FromDatabase(string value) => new(value);
}
