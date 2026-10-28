using ExploraProp.Session.Internal.ApiClient;
using SharedKernel.ApiClient;

namespace Media.Functional.StepDefinitions.Auth;

public static class AuthKeys
{
    public static readonly ScenarioKey<SwaggerResponse<CreatedSessionAuthDto>> CreateClientAuth =
        new("POST internal/v1/Auth/clients");

    public static readonly ScenarioKey<Dictionary<string, Guid>> AuthIdsByEmail =
        new("AuthIdsByEmail");

    public static readonly ScenarioKey<Dictionary<string, Guid>> ClientUserIdsByEmail =
        new("ClientUserIdsByEmail");

    public static readonly ScenarioKey<string> PrimaryAccessToken =
        new("PrimaryAccessToken");

    public static readonly ScenarioKey<string> SecondaryAccessToken =
        new("SecondaryAccessToken");

    public static readonly ScenarioKey<ProblemDetails> ProblemDetails =
        new("ProblemDetails");
}
