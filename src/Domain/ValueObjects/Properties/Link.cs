using Media.Domain.ValueObjects.Errors;
using SharedKernel.Kernel.Results;
using System.Diagnostics.CodeAnalysis;

namespace Media.Domain.ValueObjects.Properties
{
    public sealed partial record Link : IEquatable<Link>
    {

        public string Value { get; private set; } = null!;

        [ExcludeFromCodeCoverage]
        private Link() { Value = null!; }
        private Link(string value) => Value = value;

        public static Result<Link> Create(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result<Link>.Failure(LinkError.Empty);
            }

            var isValid = Uri.TryCreate(value, UriKind.Absolute, out var uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!isValid)
            {
                return Result<Link>.Failure(LinkError.InvalidFormat);
            }
            return Result<Link>.Success(new Link(value));
        }

        [ExcludeFromCodeCoverage]
        public static Link FromDatabase(string value) => new(value);
    }
}
