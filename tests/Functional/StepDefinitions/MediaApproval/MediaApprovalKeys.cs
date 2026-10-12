using ExploraProp.Media.ApiClient;

namespace Media.Functional.StepDefinitions.MediaApproval;

public static class MediaApprovalKeys
{
    public static readonly ScenarioKey<SwaggerResponse<MediaFileDto>> ApproveMedia =
        new("POST ApproveMediaCommand");
}
