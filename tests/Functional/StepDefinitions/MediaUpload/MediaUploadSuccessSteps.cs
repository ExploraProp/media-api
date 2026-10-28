using System.Net;
using ExploraProp.Media.ApiClient;
using FluentAssertions;
using Media.Functional.StepDefinitions.MediaApproval;
using Reqnroll;

namespace Media.Functional.StepDefinitions.MediaUpload;

[Binding]
[Scope(Feature = "MediaUpload", Scenario = "uploads a photo")]
[Scope(Feature = "MediaUpload", Scenario = "uploaded photo awaits virus scanning")]
[Scope(Feature = "MediaUpload", Scenario = "looks up uploaded file status")]
[Scope(Feature = "MediaUpload", Scenario = "receives fileApproved scan event")]
[Scope(Feature = "MediaApproval", Scenario = "approves a clean file with image variants")]
public sealed class MediaUploadSuccessSteps(ScenarioContext scenarioContext)
{
    private readonly ScenarioContext _scenarioContext = scenarioContext;

    [Then(@"they should see the media file")]
    public void ThenTheyShouldSeeTheMediaFile(Table table)
    {
        var result = ResolveMediaResponse();
        result.StatusCode.Should().BeOneOf(
            [(int)HttpStatusCode.OK, (int)HttpStatusCode.Created],
            because: "media upload/get/approve should return 2xx with a body");
        result.Result.Should().NotBeNull(because: "media response should include the media file DTO");

        foreach (var expected in table.CreateSet<MediaSeeRow>())
        {
            if (!string.IsNullOrWhiteSpace(expected.Status))
            {
                result.Result!.Status.Should().Be(
                    expected.Status,
                    because: "media file status should match");
            }

            if (expected.VariantCount is int count)
            {
                result.Result!.Variants.Should().NotBeNull();
                result.Result.Variants.Should().HaveCount(
                    count,
                    because: $"approved media should have {count} variants");
                result.Result.Variants.Should().OnlyContain(
                    v => !string.IsNullOrWhiteSpace(v.Url),
                    because: "every variant must have a storage URL");
                if (count == 4)
                {
                    result.Result.Variants.Select(v => v.Type).Should().BeEquivalentTo(
                        ["thumbnail", "medium", "large", "original"],
                        because: "all four variant types must be present");
                }
            }
        }
    }

    [Then(@"they should see the media scan event")]
    public void ThenTheyShouldSeeTheMediaScanEvent(Table table)
    {
        var body = _scenarioContext.Get(MediaUploadKeys.ScanEventBody);
        body.Should().NotBeNullOrWhiteSpace(because: "scan event stream should return a body");

        foreach (var expected in table.CreateSet<ScanEventRow>())
        {
            if (!string.IsNullOrWhiteSpace(expected.Name))
            {
                body.Should().Contain(
                    expected.Name,
                    because: $"SSE stream must contain {expected.Name}");
            }
        }
    }

    private SwaggerResponse<MediaFileDto> ResolveMediaResponse()
    {
        if (_scenarioContext.TryGetValue(MediaApprovalKeys.ApproveMedia, out var approved))
            return approved;
        if (_scenarioContext.TryGetValue(MediaUploadKeys.GetMedia, out var got))
            return got;
        if (_scenarioContext.TryGetValue(MediaUploadKeys.UploadMedia, out var uploaded))
            return uploaded;

        if (_scenarioContext.TryGetValue(MediaUploadKeys.ProblemDetails, out var problem))
        {
            throw new InvalidOperationException(
                $"Expected media DTO but got ProblemDetails status={problem.Status} code={problem.Code} detail={problem.Detail}");
        }

        throw new InvalidOperationException("No media upload/get/approve response was stored on the scenario.");
    }

    private sealed class MediaSeeRow
    {
        public string Status { get; set; } = "";
        public int? VariantCount { get; set; }
    }

    private sealed class ScanEventRow
    {
        public string Name { get; set; } = "";
    }
}
