using ExploraProp.Session.Internal.ApiClient;
using FluentAssertions;
using Functional.Application;
using Functional.Infrastructure;
using Reqnroll;
using SharedKernel.ApiClient;
using System.Net;
using CreateAuthRequest = ExploraProp.Session.Internal.ApiClient.CreateAuthRequest;

namespace Media.Functional.StepDefinitions.Auth;

[Binding]
[Scope(Feature = "MediaUpload")]
[Scope(Feature = "MediaApproval")]
public sealed class AuthCommonSteps(
    ScenarioContext scenarioContext,
    SessionInternalApiClient sessionInternalApi,
    TestHttpHeaders headers)
{
    private readonly ScenarioContext _scenarioContext = scenarioContext;
    private readonly SessionInternalApiClient _sessionInternalApi = sessionInternalApi;
    private readonly TestHttpHeaders _headers = headers;

    [Given(@"prefers (.*)")]
    public void GivenPrefersLanguage(string language) => _headers.SetAcceptLanguage(language);

    [Given(@"creates the following client auths:")]
    public async Task GivenCreatesTheFollowingClientAuths(Table table)
    {
        foreach (var dto in table.CreateSet<CreateAuthRequest>())
        {
            if (string.IsNullOrWhiteSpace(dto.RepeatPassword))
                dto.RepeatPassword = dto.Password;

            try
            {
                var result = await _sessionInternalApi.ClientsAsync(dto);
                result.StatusCode.Should().Be((int)HttpStatusCode.OK,
                    because: $"creating client auth for {dto.Email} should succeed");
                _scenarioContext.Put(AuthKeys.CreateClientAuth, result);
                RememberAuth(dto.Email, result.Result!.AuthId);
            }
            catch (ApiException<ProblemDetails> ex) when (ex.StatusCode == (int)HttpStatusCode.Conflict)
            {
                RememberAuth(dto.Email, Guid.NewGuid());
            }
        }
    }

    [Given(@"signs in the following consumers:")]
    public void SignsInTheFollowingConsumers(Table table)
    {
        var emails = EmailsFrom(table).ToList();
        emails.Should().NotBeEmpty(because: "sign-in table must list at least one email");
        foreach (var email in emails)
            AuthorizeEmail(email, stashAsSecondary: false);
        _scenarioContext.Has(AuthKeys.PrimaryAccessToken).Should().BeTrue(
            because: "primary JWT must be stored after consumer sign-in");
        _scenarioContext["SecondaryAuthActive"] = false;
    }

    [Given(@"signs in the secondary consumer:")]
    public void SignsInSecondaryConsumer(Table table)
    {
        var emails = EmailsFrom(table).ToList();
        emails.Should().NotBeEmpty(because: "secondary sign-in table must list at least one email");
        foreach (var email in emails)
            AuthorizeEmail(email, stashAsSecondary: true);
        _scenarioContext.Has(AuthKeys.SecondaryAccessToken).Should().BeTrue(
            because: "secondary JWT must be stored after secondary sign-in");
        _scenarioContext["SecondaryAuthActive"] = true;
    }

    [Given(@"clears the auth token")]
    [When(@"clears the auth token")]
    public void ClearsTheAuthToken() => _headers.ClearAuthToken();

    [Given(@"restores the primary auth token")]
    [When(@"restores the primary auth token")]
    public void RestoresThePrimaryAuthToken()
    {
        var token = (string)_scenarioContext[AuthKeys.PrimaryAccessToken.Value];
        token.Should().NotBeNullOrWhiteSpace();
        _headers.SetAuthToken(token);
        _scenarioContext["SecondaryAuthActive"] = false;
    }

    [Given(@"restores the secondary auth token")]
    [When(@"restores the secondary auth token")]
    public void RestoresTheSecondaryAuthToken()
    {
        var token = (string)_scenarioContext[AuthKeys.SecondaryAccessToken.Value];
        token.Should().NotBeNullOrWhiteSpace();
        _headers.SetAuthToken(token);
        _scenarioContext["SecondaryAuthActive"] = true;
    }

    private void AuthorizeEmail(string email, bool stashAsSecondary)
    {
        var authId = TryGetAuthId(email) ?? Guid.NewGuid();
        RememberAuth(email, authId);
        var clientUserId = Guid.NewGuid();
        RememberClientUserId(email, clientUserId);
        var token = TestJwtFactory.CreateClientToken(authId, email, clientUserId);
        token.Should().NotBeNullOrWhiteSpace(because: "minted JWT must not be empty");

        var key = stashAsSecondary
            ? AuthKeys.SecondaryAccessToken.Value
            : AuthKeys.PrimaryAccessToken.Value;
        _scenarioContext[key] = token;
        _headers.SetAuthToken(token);
    }

    private static IEnumerable<string> EmailsFrom(Table table)
    {
        foreach (var row in table.Rows)
        {
            string? email = null;
            if (row.ContainsKey("email"))
                email = row["email"];
            else if (row.Count > 0)
                email = row[0];

            if (!string.IsNullOrWhiteSpace(email))
                yield return email.Trim();
        }
    }

    private void RememberAuth(string email, Guid authId)
    {
        if (!_scenarioContext.TryGetValue(AuthKeys.AuthIdsByEmail.Value, out var boxed)
            || boxed is not Dictionary<string, Guid> map)
        {
            map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            _scenarioContext[AuthKeys.AuthIdsByEmail.Value] = map;
        }

        map[email] = authId;
    }

    private void RememberClientUserId(string email, Guid clientUserId)
    {
        if (!_scenarioContext.TryGetValue(AuthKeys.ClientUserIdsByEmail.Value, out var boxed)
            || boxed is not Dictionary<string, Guid> map)
        {
            map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            _scenarioContext[AuthKeys.ClientUserIdsByEmail.Value] = map;
        }

        map[email] = clientUserId;
    }

    private Guid? TryGetAuthId(string email)
    {
        if (_scenarioContext.TryGetValue(AuthKeys.AuthIdsByEmail.Value, out var boxed)
            && boxed is Dictionary<string, Guid> map
            && map.TryGetValue(email, out var id))
            return id;
        return null;
    }
}
