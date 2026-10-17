using Functional.Infrastructure;
using SharedKernel.Infrastructure.Services;

namespace Media.Functional.Infrastructure;

/// <summary>
/// Mints mesh JWTs with the functional-test secret so Media SUT auth works without
/// waiting on Customer→Session AuthProfileLink for <c>client_user_id</c>.
/// </summary>
public static class TestJwtFactory
{
    private static readonly JwtService Jwt = new(
        FunctionalTestSecrets.JwtSecretKey,
        FunctionalTestSecrets.JwtIssuer,
        FunctionalTestSecrets.JwtAudience);

    public static string CreateClientToken(Guid authId, string email, Guid clientUserId) =>
        Jwt.Create(
            authId,
            email,
            emailVerified: true,
            role: "Client",
            clientUserId: clientUserId);
}
