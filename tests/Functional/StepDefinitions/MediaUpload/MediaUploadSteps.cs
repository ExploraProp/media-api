using System.Net.Http.Headers;
using System.Text.Json;
using ExploraProp.Media.ApiClient;
using FluentAssertions;
using Functional.Application;
using Functional.Domain.Enums.Properties;
using Functional.Domain.ValueObjects.Properties;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using SharedKernel.ApiClient;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Media.Functional.StepDefinitions.MediaUpload;

[Binding]
[Scope(Feature = "MediaUpload")]
[Scope(Feature = "MediaApproval")]
public sealed class MediaUploadSteps(
    ScenarioContext scenarioContext,
    MediaApiClient mediaApi,
    FeatureContext featureContext)
{
    private readonly ScenarioContext _scenarioContext = scenarioContext;
    private readonly MediaApiClient _mediaApi = mediaApi;
    private readonly Worker _worker = featureContext.Get<Worker>();

    [Given(@"uploads a photo to the listing")]
    public async Task GivenUploadsAPhotoToTheListing()
    {
        await UploadAsync(CreateValidJpegStream(), "photo.jpg", "image/jpeg");
        _scenarioContext.Has(MediaUploadKeys.MediaFileId).Should().BeTrue(
            because: DescribeUploadFailure());
    }

    [When(@"uploads a photo to the listing")]
    public Task WhenUploadsAPhotoToTheListing() =>
        UploadAsync(CreateValidJpegStream(), "photo.jpg", "image/jpeg");

    [When(@"uploads an oversized photo to the listing")]
    public Task UploadsAnOversizedPhotoToTheListing() =>
        // 10 MB exceeds the seeded listing policy (8 MB) but stays under the controller request limit (16 MB).
        UploadAsync(CreateOversizedJpegPlaceholder(10 * 1024 * 1024), "large.jpg", "image/jpeg");

    [When(@"uploads a disallowed file to the listing")]
    public Task UploadsADisallowedFileToTheListing()
    {
        var bytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
        return UploadAsync(new MemoryStream(bytes), "malware.exe", "application/octet-stream");
    }

    [Given(@"looks up the uploaded media file")]
    [When(@"looks up the uploaded media file")]
    public async Task LooksUpTheUploadedMediaFile()
    {
        var id = _scenarioContext.Get(MediaUploadKeys.MediaFileId);
        await LookUpMediaAsync(id);
    }

    [When(@"looks up media file ""(.*)""")]
    public Task LooksUpMediaFile(string mediaFileId) => LookUpMediaAsync(mediaFileId);

    [When(@"subscribes to scan events for the uploaded media file")]
    public async Task SubscribesToScanEventsForTheUploadedMediaFile()
    {
        var id = _scenarioContext.Get(MediaUploadKeys.MediaFileId);
        await SubscribeToEventsAsync(id);
    }

    [When(@"subscribes to scan events for media file ""(.*)""")]
    public Task SubscribesToScanEventsForMediaFile(string mediaFileId) =>
        SubscribeToEventsAsync(mediaFileId);

    private async Task UploadAsync(Stream stream, string fileName, string contentType)
    {
        var listingId = _scenarioContext.Get(MediaUploadKeys.ListingOwnerId);
        var baseUrl = HostName.For(ApplicationProgrammingInterface.Media).BaseUrl.TrimEnd('/');

        await using (stream)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(OwnerType.Listing.ToI18nCode()), "OwnerType");
            content.Add(new StringContent(listingId.ToString()), "OwnerId");
            content.Add(new StringContent("photo"), "MediaType");

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "File", fileName);

            using var response = await _worker.HttpClient.PostAsync(
                $"{baseUrl}/api/v1/Media/upload",
                content);
            var body = await response.Content.ReadAsStringAsync();

            if ((int)response.StatusCode is >= 400)
            {
                try
                {
                    var problem = JsonSerializer.Deserialize<ProblemDetails>(
                        body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (problem is not null)
                        _scenarioContext.Put(MediaUploadKeys.ProblemDetails, problem);
                }
                catch (JsonException)
                {
                    // leave ProblemDetails unset; callers assert on presence when needed
                }

                return;
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = doc.RootElement;
            var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;

            if (string.IsNullOrWhiteSpace(id))
                id = await FindLatestMediaIdForListingAsync(listingId);

            id.Should().NotBeNullOrWhiteSpace(
                because: $"upload {(int)response.StatusCode} body must include id; body='{body}'");

            var dto = new MediaFileDto
            {
                Id = id!,
                Status = status ?? "",
                StatusLabel = root.TryGetProperty("statusLabel", out var sl) ? sl.GetString() ?? "" : "",
                PublicUrl = root.TryGetProperty("publicUrl", out var pu) ? pu.GetString() : null,
                OwnerType = root.TryGetProperty("ownerType", out var ot) ? ot.GetString() ?? "" : "",
                OwnerTypeLabel = root.TryGetProperty("ownerTypeLabel", out var otl) ? otl.GetString() ?? "" : "",
                MediaType = root.TryGetProperty("mediaType", out var mt) ? mt.GetString() ?? "" : "",
                MediaTypeLabel = root.TryGetProperty("mediaTypeLabel", out var mtl) ? mtl.GetString() ?? "" : "",
                UploadedAt = root.TryGetProperty("uploadedAt", out var ua) ? ua.GetString() ?? "" : "",
                CreatedAt = root.TryGetProperty("createdAt", out var ca) ? ca.GetString() ?? "" : "",
                UpdatedAt = root.TryGetProperty("updatedAt", out var uat) ? uat.GetString() ?? "" : "",
            };

            _scenarioContext.Put(
                MediaUploadKeys.UploadMedia,
                new SwaggerResponse<MediaFileDto>(
                    (int)response.StatusCode,
                    new Dictionary<string, IEnumerable<string>>(),
                    dto));
            _scenarioContext.Remove(MediaUploadKeys.ProblemDetails);
            _scenarioContext.Put(MediaUploadKeys.MediaFileId, id!);
        }
    }

    private async Task LookUpMediaAsync(string mediaFileId)
    {
        try
        {
            var result = await _mediaApi.MediaGETAsync(mediaFileId);
            _scenarioContext.Put(MediaUploadKeys.GetMedia, result);
            _scenarioContext.Remove(MediaUploadKeys.ProblemDetails);
            if (result.Result?.Id is { Length: > 0 } id)
                _scenarioContext.Put(MediaUploadKeys.MediaFileId, id);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _scenarioContext.Put(MediaUploadKeys.ProblemDetails, ex.Result);
        }
    }

    private async Task SubscribeToEventsAsync(string mediaFileId)
    {
        var baseUrl = HostName.For(ApplicationProgrammingInterface.Media).BaseUrl.TrimEnd('/');
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/api/v1/Media/{Uri.EscapeDataString(mediaFileId)}/events");

        try
        {
            using var response = await _worker.HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead);

            if ((int)response.StatusCode is >= 200 and < 300)
            {
                var body = response.Content is null
                    ? ""
                    : await response.Content.ReadAsStringAsync();
                _scenarioContext.Put(
                    MediaUploadKeys.ScanEvents,
                    new SwaggerResponse((int)response.StatusCode, new Dictionary<string, IEnumerable<string>>()));
                _scenarioContext.Put(MediaUploadKeys.ScanEventBody, body);
                _scenarioContext.Remove(MediaUploadKeys.ProblemDetails);
                return;
            }

            await _mediaApi.EventsAsync(mediaFileId);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _scenarioContext.Put(MediaUploadKeys.ProblemDetails, ex.Result);
        }
    }

    private async Task<string?> FindLatestMediaIdForListingAsync(Guid listingId)
    {
        await using var scope = _worker.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var ownerId = new OwnerId(listingId);
        var rows = await db.MediaFile.AsNoTracking().ToListAsync();
        return rows
            .Where(m => m.OwnerId == ownerId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => m.Id.ToString())
            .FirstOrDefault();
    }

    private string DescribeUploadFailure()
    {
        if (_scenarioContext.TryGetValue(MediaUploadKeys.ProblemDetails, out var problem))
            return $"upload failed status={problem.Status} code={problem.Code} detail={problem.Detail}";
        if (_scenarioContext.TryGetValue(MediaUploadKeys.UploadMedia, out var uploaded))
            return $"upload stored status={uploaded.StatusCode} id='{uploaded.Result?.Id}'";
        return "upload did not store MediaFileId or ProblemDetails";
    }

    private static MemoryStream CreateValidJpegStream()
    {
        var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(128, 128))
            image.SaveAsJpeg(stream);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateOversizedJpegPlaceholder(int size)
    {
        var stream = new MemoryStream(size);
        stream.Write([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01]);
        while (stream.Length < size)
            stream.WriteByte(0x00);
        stream.Position = 0;
        return stream;
    }
}
