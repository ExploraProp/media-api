using System.Net;
using FluentAssertions;
using SharedKernel.ApiClient;
using SharedKernel.Kernel.Results;

namespace Media.Functional.Infrastructure;

public static class ProblemDetailsAssertions
{
    public static void ShouldContainErrorCodes(
        ProblemDetails result,
        IEnumerable<ProblemDetailsError> expectedErrors)
    {
        foreach (var expected in expectedErrors)
        {
            if (string.IsNullOrWhiteSpace(expected.Code))
                continue;

            var expectedCode = ErrorCode.Parse(expected.Code);
            if (result.Status == (int)HttpStatusCode.UnprocessableEntity)
            {
                result.Errors.Should().NotBeNull().And.NotBeEmpty();
                result.Errors!
                    .Select(e => ErrorCode.Parse(e.Code))
                    .Should()
                    .Contain(expectedCode, because: $"error should be {expectedCode.Value}");
            }
            else
            {
                ErrorCode.Parse(result.Code)
                    .Should()
                    .Be(expectedCode, because: $"error should be {expectedCode.Value}");
            }
        }
    }
}
