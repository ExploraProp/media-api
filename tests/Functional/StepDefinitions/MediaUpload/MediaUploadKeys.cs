using ExploraProp.Media.ApiClient;
using SharedKernel.ApiClient;

namespace Media.Functional.StepDefinitions.MediaUpload;

public static class MediaUploadKeys
{
    public static readonly ScenarioKey<SwaggerResponse<MediaFileDto>> UploadMedia =
        new("POST /api/v1/Media/upload");

    public static readonly ScenarioKey<SwaggerResponse<MediaFileDto>> GetMedia =
        new("GET /api/v1/Media/{id}");

    public static readonly ScenarioKey<SwaggerResponse> DeleteMedia =
        new("DELETE /api/v1/Media/{id}");

    public static readonly ScenarioKey<SwaggerResponse> ScanEvents =
        new("GET /api/v1/Media/{id}/events");

    public static readonly ScenarioKey<string> ScanEventBody =
        new("GET /api/v1/Media/{id}/events/body");

    public static readonly ScenarioKey<ProblemDetails> ProblemDetails =
        new("ProblemDetails");

    public static readonly ScenarioKey<Guid> ListingOwnerId =
        new("ListingOwnerId");

    public static readonly ScenarioKey<string> MediaFileId =
        new("MediaFileId");
}
