using System.Net;
using FluentAssertions;
using Reqnroll;
using SharedKernel.ApiClient;

namespace Media.Functional.StepDefinitions.MediaUpload;

[Binding]
[Scope(Feature = "MediaUpload", Scenario = "oversized photo is rejected")]
[Scope(Feature = "MediaUpload", Scenario = "disallowed file type is rejected")]
[Scope(Feature = "MediaUpload", Scenario = "photo limit is rejected")]
[Scope(Feature = "MediaUpload", Scenario = "looks up unknown media file")]
[Scope(Feature = "MediaUpload", Scenario = "anonymous visitor cannot subscribe to scan events")]
[Scope(Feature = "MediaUpload", Scenario = "subscribes to events for unknown media file")]
[Scope(Feature = "MediaUpload", Scenario = "malformed media id is rejected for scan events")]
[Scope(Feature = "MediaUpload", Scenario = "secondary consumer cannot subscribe to scan events")]
[Scope(Feature = "MediaApproval", Scenario = "approving unknown media file is not found")]
[Scope(Feature = "MediaApproval", Scenario = "deletes a media file")]
[Scope(Feature = "MediaApproval", Scenario = "deleting unknown media file is not found")]
[Scope(Feature = "MediaApproval", Scenario = "secondary consumer cannot delete media file")]
[Scope(Feature = "MediaApproval", Scenario = "scanning media file cannot be deleted")]
public sealed class MediaUploadFailureSteps(ScenarioContext scenarioContext)
{
    private readonly ScenarioContext _scenarioContext = scenarioContext;

    [Then(@"they should see the media error")]
    public void ThenTheyShouldSeeTheMediaError(Table table)
    {
        var result = _scenarioContext.Get(MediaUploadKeys.ProblemDetails);
        result.Should().NotBeNull();
        result.Status.Should().NotBeNull();
        result.Status!.Value.Should().BeOneOf(
            (int)HttpStatusCode.Unauthorized,
            (int)HttpStatusCode.Forbidden,
            (int)HttpStatusCode.NotFound,
            (int)HttpStatusCode.Conflict,
            (int)HttpStatusCode.UnprocessableEntity,
            (int)HttpStatusCode.BadRequest);

        ProblemDetailsAssertions.ShouldContainErrorCodes(
            result,
            table.CreateSet<ProblemDetailsError>());
    }
}
