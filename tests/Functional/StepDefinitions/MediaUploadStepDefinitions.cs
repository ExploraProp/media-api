using ExploraProp.Media.ApiClient;
using FluentAssertions;
using Functional.Infrastructure;
using Functional.StepDefinitions.Shared;
using Functional.Support.TestData;
using Media.Application.Dto;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Reqnroll;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
using System.Text.Json;
using KiotaMediaFileDto = ExploraProp.Media.ApiClient.Models.MediaFileDto;
using KiotaMediaVariantDto = ExploraProp.Media.ApiClient.Models.MediaVariantDto;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;
using UploadMediaType = Media.Domain.Enums.Properties.MediaType;

namespace Media.Functional.StepDefinitions;

[Binding]
public sealed class MediaUploadStepDefinitions(
    ScenarioContext scenarioContext,
    ApiClient apiClient,
    TestHttpHeaders headers,
    MediaSseClient mediaSse,
    MediaApiClient mediaApi)
{
    private readonly ScenarioContext _scenarioContext = scenarioContext;
    private readonly ApiClient _apiClient = apiClient;
    private readonly TestHttpHeaders _headers = headers;
    private readonly MediaSseClient _mediaSse = mediaSse;
    private readonly MediaApiClient _mediaApi = mediaApi;

    internal const string LastMediaFileDtoKey = "LastMediaFileDto";

    private Guid ListingId => Guid.Parse(_scenarioContext.Get<string>("ListingId"));

    [Given(@"Carlos's listing already has the maximum number of photos")]
    public async Task GivenCarlosListingAlreadyHasMaximumPhotos()
    {
        var factory = _scenarioContext.Get<ExploraPropWebApplicationFactory>("WebApplicationFactory");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();

        for (var i = 0; i < 50; i++)
        {
            var mediaFile = MediaFile.Create(
                MediaFileId.New(),
                new OwnerId(ListingId),
                OwnerType.Listing,
                UploadMediaType.Photo,
                MediaStatus.Pending,
                StoragePath.FromDatabase($"quarantine/listing/{ListingId}/{Guid.NewGuid()}.jpg"),
                AllowedContentTypes.Jpeg,
                MediaFileSize.FromDatabase(1024),
                UploadedByClientId.New()).Value!;
            db.MediaFile.Add(mediaFile);
        }

        await db.SaveChangesAsync();
    }

    [Given(@"Carlos uploaded a photo to his listing")]
    public async Task GivenCarlosUploadedAPhotoToHisListing()
    {
        await EnsureCarlosSignedInAsync();
        var result = await UploadPhotoAsync(CreateJpegStream(2 * 1024 * 1024), "photo.jpg", "image/jpeg");
        result.IsSuccess.Should().BeTrue(
            $"seed upload failed: {result.Error?.Detail ?? result.RawContent}");
        _scenarioContext[LastMediaFileDtoKey] = result.Data!;
    }

    [When(@"Carlos uploads a photo to his listing")]
    public async Task WhenCarlosUploadsAPhotoToHisListing()
    {
        await UploadForListingAsync(CreateJpegStream(2 * 1024 * 1024), "photo.jpg", "image/jpeg");
    }

    [When(@"Carlos uploads an oversized photo to his listing")]
    public async Task WhenCarlosUploadsAnOversizedPhotoToHisListing()
    {
        // 10 MB exceeds the seeded listing policy (8 MB) but stays under the controller request limit (16 MB).
        await UploadForListingAsync(CreateJpegStream(10 * 1024 * 1024), "large.jpg", "image/jpeg");
    }

    [When(@"Carlos uploads a disallowed file to his listing")]
    public async Task WhenCarlosUploadsADisallowedFileToHisListing()
    {
        var bytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
        await using var stream = new MemoryStream(bytes);
        await UploadForListingAsync(stream, "malware.exe", "application/octet-stream");
    }

    [When(@"Carlos uploads another photo to his listing")]
    public async Task WhenCarlosUploadsAnotherPhotoToHisListing()
    {
        await UploadForListingAsync(CreateJpegStream(1024), "another.jpg", "image/jpeg");
    }

    [When(@"Carlos checks the status of his uploaded file")]
    public async Task WhenCarlosChecksTheStatusOfHisUploadedFile()
    {
        var media = _scenarioContext.Get<MediaFileDto>(LastMediaFileDtoKey);
        var result = await GetMediaAsync(media.Id);
        _scenarioContext.StashLastResponse(result);
        if (result.IsSuccess && result.Data is not null)
            _scenarioContext[LastMediaFileDtoKey] = result.Data;
    }

    [When(@"Carlos checks the status of a file that does not exist")]
    public async Task WhenCarlosChecksTheStatusOfAFileThatDoesNotExist()
    {
        await EnsureCarlosSignedInAsync();
        var result = await GetMediaAsync(Guid.NewGuid().ToString());
        _scenarioContext.StashLastResponse(result);
    }

    [When(@"an anonymous visitor subscribes to scan events for the file")]
    public async Task WhenAnAnonymousVisitorSubscribesToScanEventsForTheFile()
    {
        _headers.ClearAuthToken();
        var media = _scenarioContext.Get<MediaFileDto>(LastMediaFileDtoKey);
        // SSE is hand-written (OpenAPI documents event-stream; Kiota does not generate a usable client).
        var result = await _mediaSse.GetEventsAsync(Guid.Parse(media.Id));
        _scenarioContext.StashLastResponse(result);
    }

    [When(@"Carlos subscribes to scan events for a file that does not exist")]
    public async Task WhenCarlosSubscribesToScanEventsForAFileThatDoesNotExist()
    {
        await EnsureCarlosSignedInAsync();
        var result = await _mediaSse.GetEventsAsync(Guid.NewGuid());
        _scenarioContext.StashLastResponse(result);
    }

    [When(@"Carlos subscribes to scan events for a malformed media id")]
    public async Task WhenCarlosSubscribesToScanEventsForAMalformedMediaId()
    {
        await EnsureCarlosSignedInAsync();
        var result = await _mediaSse.GetEventsAsync("not-a-guid");
        _scenarioContext.StashLastResponse(result);
    }

    [When(@"Carlos subscribes to scan events for his uploaded file")]
    public async Task WhenCarlosSubscribesToScanEventsForHisUploadedFile()
    {
        await EnsureCarlosSignedInAsync();
        var media = _scenarioContext.Get<MediaFileDto>(LastMediaFileDtoKey);
        var result = await _mediaSse.GetEventsAsync(Guid.Parse(media.Id));
        _scenarioContext.StashLastResponse(result);
        _scenarioContext["LastSseResponse"] = result.RawContent ?? "";
    }

    [Then(@"Carlos should receive a fileApproved scan event")]
    public void ThenCarlosShouldReceiveAFileApprovedScanEvent()
    {
        var sseContent = _scenarioContext.Get<string>("LastSseResponse");
        sseContent.Should().Contain("fileApproved",
            "the SSE stream must contain a fileApproved event because the file was already approved");
    }

    [When(@"María subscribes to Carlos's uploaded file scan events")]
    [When(@"Maria subscribes to Carlos's uploaded file scan events")]
    public async Task WhenMariaSubscribesToCarlossUploadedFileScanEvents()
    {
        await EnsureMariaSignedInAsync();
        var media = _scenarioContext.Get<MediaFileDto>(LastMediaFileDtoKey);
        var result = await _mediaSse.GetEventsAsync(Guid.Parse(media.Id));
        _scenarioContext.StashLastResponse(result);
    }

    [Then(@"the uploaded file should await virus scanning")]
    public void ThenTheUploadedFileShouldAwaitVirusScanning()
    {
        var media = _scenarioContext.Get<MediaFileDto>(LastMediaFileDtoKey);
        media.Id.Should().NotBeNullOrWhiteSpace();
        media.Status.Should().Be(MediaStatus.Pending.ToI18nCode());
    }

    [Then(@"the system should reject the upload because the file is too large")]
    public void ThenTheSystemShouldRejectTheUploadBecauseTheFileIsTooLarge()
    {
        var snapshot = _scenarioContext.Get<LastApiResponse>("LastApiResponse");
        snapshot.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        snapshot.FieldErrors.Should().ContainSingle(f => f.Code == MediaPolicyError.TooLarge.Code.Value,
            "the 422 must carry the FileTooLarge error in the errors extension");
    }

    [Then(@"the system should reject the upload because the file type is not allowed")]
    public void ThenTheSystemShouldRejectTheUploadBecauseTheFileTypeIsNotAllowed()
    {
        var snapshot = _scenarioContext.Get<LastApiResponse>("LastApiResponse");
        snapshot.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        snapshot.FieldErrors.Should().ContainSingle(f => f.Code == MediaPolicyError.UnsupportedContentType.Code.Value,
            "the 422 must carry the UnsupportedContentType error in the errors extension");
    }

    [Then(@"the system should reject the upload because the photo limit is reached")]
    public void ThenTheSystemShouldRejectTheUploadBecauseThePhotoLimitIsReached()
    {
        var snapshot = _scenarioContext.Get<LastApiResponse>("LastApiResponse");
        snapshot.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        snapshot.FieldErrors.Should().ContainSingle(f => f.Code == MediaPolicyError.TooMany.Code.Value,
            "the 422 must carry the TooMany error in the errors extension");
    }

    [Then(@"the media file should not be found")]
    public void ThenMediaFileShouldNotBeFound()
    {
        var snapshot = _scenarioContext.Get<LastApiResponse>("LastApiResponse");
        snapshot.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task UploadForListingAsync(Stream stream, string fileName, string contentType)
    {
        await EnsureCarlosSignedInAsync();
        var result = await UploadPhotoAsync(stream, fileName, contentType);
        _scenarioContext.StashLastResponse(result);
        if (result.IsSuccess && result.Data is not null)
            _scenarioContext[LastMediaFileDtoKey] = result.Data;
    }

    private async Task<ApiResponse<MediaFileDto>> UploadPhotoAsync(
        Stream stream,
        string fileName,
        string contentType)
    {
        // Multipart validation errors: keep shared ApiClient so ProblemDetails field codes
        // round-trip into FieldErrors (Kiota Media ProblemDetails lacks typed Errors).
        return (await _apiClient.UploadMediaAsync(
            stream,
            fileName,
            contentType,
            OwnerType.Listing.ToI18nCode(),
            ListingId)).As<MediaFileDto>();
    }

    private async Task<ApiResponse<MediaFileDto>> GetMediaAsync(string mediaFileId)
    {
        var result = await KiotaApiCall.TryAsync(() =>
            _mediaApi.Api.V1.Media[mediaFileId].GetAsync());
        return MapMediaResponse(result);
    }

    private static ApiResponse<MediaFileDto> MapMediaResponse(ApiResponse<KiotaMediaFileDto> result) =>
        new()
        {
            StatusCode = result.StatusCode,
            IsSuccess = result.IsSuccess,
            Data = result.Data is null ? null : ToAppDto(result.Data),
            Error = result.Error,
            RawContent = result.RawContent,
        };

    private static MediaFileDto ToAppDto(KiotaMediaFileDto dto) =>
        new(
            dto.Id ?? "",
            dto.CreatedAt ?? "",
            dto.UpdatedAt ?? "",
            dto.Status ?? "",
            dto.StatusLabel ?? "",
            dto.PublicUrl,
            dto.OwnerType ?? "",
            dto.OwnerTypeLabel ?? "",
            dto.MediaType ?? "",
            dto.MediaTypeLabel ?? "",
            dto.UploadedAt ?? "")
        {
            Variants = (dto.Variants ?? [])
                .Select(ToAppVariant)
                .ToList()
                .AsReadOnly(),
        };

    private static MediaVariantDto ToAppVariant(KiotaMediaVariantDto v) =>
        new(
            v.Type ?? "",
            v.TypeLabel ?? "",
            v.Url ?? "",
            null,
            null,
            0);

    private Task EnsureCarlosSignedInAsync() => EnsurePersonaSignedInAsync(Personas.Carlos);

    private Task EnsureMariaSignedInAsync() => EnsurePersonaSignedInAsync(Personas.Maria);

    private async Task EnsurePersonaSignedInAsync(Persona persona)
    {
        var tokenKey = $"AccessToken:{persona.Email}";
        if (_scenarioContext.TryGetValue(tokenKey, out string? cached) && !string.IsNullOrEmpty(cached))
        {
            _headers.SetAuthToken(cached);
            return;
        }

        var login = await _apiClient.LoginAsync(new { email = persona.Email, password = persona.Password });
        login.IsSuccess.Should().BeTrue($"{persona.FirstName} login failed: {login.Error?.Detail ?? login.RawContent}");
        var jwt = login.Data.ValueKind == JsonValueKind.Object
            && login.Data.TryGetProperty("accessToken", out var access)
            ? access.GetString() ?? ""
            : "";
        jwt.Should().NotBeNullOrEmpty();
        _headers.SetAuthToken(jwt);
        _scenarioContext[tokenKey] = jwt;
    }

    private static MemoryStream CreateJpegStream(int minSize) =>
        minSize > 8 * 1024 * 1024
            ? CreateOversizedJpegPlaceholder(minSize)
            : CreateValidJpegStream(minSize);

    private static MemoryStream CreateOversizedJpegPlaceholder(int size)
    {
        var stream = new MemoryStream(size);
        stream.Write([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01]);
        while (stream.Length < size)
            stream.WriteByte(0x00);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateValidJpegStream(int minSize)
    {
        var dimension = 64;
        MemoryStream stream;
        do
        {
            stream = new MemoryStream();
            using var image = new Image<Rgba32>(dimension, dimension);
            image.SaveAsJpeg(stream);
            if (stream.Length >= minSize)
                break;
            dimension *= 2;
        } while (dimension <= 8192);

        stream.Position = 0;
        return stream;
    }
}
