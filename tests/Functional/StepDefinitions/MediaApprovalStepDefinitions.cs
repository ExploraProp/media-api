using ExploraProp.Media.ApiClient;
using FluentAssertions;
using Functional.Infrastructure;
using Functional.StepDefinitions.Shared;
using Functional.Support.TestData;
using Media.Application.Commands;
using Media.Application.Dto;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using SharedKernel.Kernel.Results;
using System.Net;
using System.Text.Json;

namespace Media.Functional.StepDefinitions;

[Binding]
public sealed class MediaApprovalStepDefinitions(
    ScenarioContext scenarioContext,
    ApiClient apiClient,
    MediaApiClient mediaApi)
{
    [Given(@"the scanner approved Carlos's uploaded photo")]
    public async Task GivenTheScannerApprovedCarlosUploadedPhoto()
    {
        var media = scenarioContext.Get<MediaFileDto>(MediaUploadStepDefinitions.LastMediaFileDtoKey);
        var mediaFileId = Guid.Parse(media.Id);
        var dto = await ApproveAsync(mediaFileId);
        if (dto is not null)
            scenarioContext[MediaUploadStepDefinitions.LastMediaFileDtoKey] = dto;
    }

    [When(@"the scanner approves Carlos's uploaded photo")]
    public async Task WhenTheScannerApprovesCarlosUploadedPhoto()
    {
        var media = scenarioContext.Get<MediaFileDto>(MediaUploadStepDefinitions.LastMediaFileDtoKey);
        var mediaFileId = Guid.Parse(media.Id);
        var result = await SendApproveCommandAsync(mediaFileId);
        var apiResponse = ToApiResponse(result);
        scenarioContext.StashLastResponse(apiResponse);
        if (result.IsSuccess && result.Value is not null)
            scenarioContext[MediaUploadStepDefinitions.LastMediaFileDtoKey] = result.Value;
    }

    [When(@"the scanner approves a file that does not exist")]
    public async Task WhenTheScannerApprovesAFileThatDoesNotExist()
    {
        var result = await SendApproveCommandAsync(Guid.NewGuid());
        scenarioContext.StashLastResponse(ToApiResponse(result));
    }

    [When(@"Carlos deletes his uploaded photo")]
    public async Task WhenCarlosDeletesHisUploadedPhoto()
    {
        var media = scenarioContext.Get<MediaFileDto>(MediaUploadStepDefinitions.LastMediaFileDtoKey);
        await DeleteMediaAsync(media.Id);
    }

    [When(@"Carlos deletes a file that does not exist")]
    public async Task WhenCarlosDeletesAFileThatDoesNotExist()
    {
        await DeleteMediaAsync(Guid.NewGuid().ToString());
    }

    [Given(@"the scanner has started scanning Carlos's photo")]
    public async Task GivenTheScannerHasStartedScanningCarlosPhoto()
    {
        var media = scenarioContext.Get<MediaFileDto>(MediaUploadStepDefinitions.LastMediaFileDtoKey);
        var factory = scenarioContext.Get<ExploraPropWebApplicationFactory>("WebApplicationFactory");
        await using var scope = factory.Services.CreateAsyncScope();
        var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepo>();
        var idResult = MediaFileId.Create(media.Id);
        idResult.IsSuccess.Should().BeTrue();
        var fileResult = await mediaRepo.GetByIdAsync(idResult.Value!, CancellationToken.None);
        fileResult.IsSuccess.Should().BeTrue();
        var file = fileResult.Value!;
        if (file.Status == MediaStatus.Pending)
        {
            file.BeginScan();
            await mediaRepo.UpdateAsync(file, CancellationToken.None);
        }
    }

    [When(@"Sofía tries to delete Carlos's photo")]
    [When(@"Sofia tries to delete Carlos's photo")]
    public async Task WhenSofiaTriesToDeleteCarlosPhoto()
    {
        await EnsureSofiaSignedInAsync();
        var media = scenarioContext.Get<MediaFileDto>(MediaUploadStepDefinitions.LastMediaFileDtoKey);
        await DeleteMediaAsync(media.Id);
    }

    [Then(@"the system should reject the deletion because the file is being scanned")]
    public void ThenSystemShouldRejectDeletionBecauseFileIsBeingScanned()
    {
        var snapshot = scenarioContext.Get<LastApiResponse>("LastApiResponse");
        snapshot.StatusCode.Should().Be(HttpStatusCode.Conflict);
        snapshot.ErrorCode.Should().Be(MediaFileError.OngoingScanning.Code.Value);
    }

    [Then(@"the media file should be approved")]
    public void ThenTheMediaFileShouldBeApproved()
    {
        var media = scenarioContext.Get<MediaFileDto>(MediaUploadStepDefinitions.LastMediaFileDtoKey);
        media.Status.Should().Be(MediaStatus.Approved.ToI18nCode());
    }

    [Then(@"the approved media file should have 4 image variants")]
    public void ThenTheApprovedMediaFileShouldHaveFourImageVariants()
    {
        var media = scenarioContext.Get<MediaFileDto>(MediaUploadStepDefinitions.LastMediaFileDtoKey);
        media.Variants.Should().HaveCount(4,
            "a JPEG should produce thumbnail, medium, large, and original variants");
        media.Variants.Should().OnlyContain(v => !string.IsNullOrWhiteSpace(v.Url),
            "every variant must have a storage URL");
        media.Variants.Select(v => v.Type).Should().BeEquivalentTo(
            ["thumbnail", "medium", "large", "original"],
            "all four variant types must be present");
    }

    [Then(@"the media file should be deleted")]
    public void ThenTheMediaFileShouldBeDeleted()
    {
        var snapshot = scenarioContext.Get<LastApiResponse>("LastApiResponse");
        snapshot.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<MediaFileDto?> ApproveAsync(Guid mediaFileId)
    {
        var result = await SendApproveCommandAsync(mediaFileId);
        return result.IsSuccess ? result.Value : null;
    }

    private async Task<Result<MediaFileDto>> SendApproveCommandAsync(Guid mediaFileId)
    {
        var factory = scenarioContext.Get<ExploraPropWebApplicationFactory>("WebApplicationFactory");
        await using var scope = factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var idResult = MediaFileId.Create(mediaFileId.ToString());
        if (idResult.IsFailure) return Result<MediaFileDto>.FromResult(idResult);

        // Mirror what ScanWorker does: transition Pending → Scanning before approving.
        var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepo>();
        var mediaFileResult = await mediaRepo.GetByIdAsync(idResult.Value!, CancellationToken.None);
        if (mediaFileResult.IsSuccess && mediaFileResult.Value!.Status == MediaStatus.Pending)
        {
            var beginResult = mediaFileResult.Value!.BeginScan();
            if (beginResult.IsSuccess)
                await mediaRepo.UpdateAsync(mediaFileResult.Value!, CancellationToken.None);
        }

        return await mediator.Send(new ApproveMediaCommand(idResult.Value!), CancellationToken.None);
    }

    private static ApiResponse<MediaFileDto> ToApiResponse(Result<MediaFileDto> result) =>
        new()
        {
            StatusCode = result.IsSuccess ? HttpStatusCode.OK : result.Error!.StatusCode,
            IsSuccess = result.IsSuccess,
            Data = result.Value,
        };

    private async Task DeleteMediaAsync(string mediaFileId)
    {
        var result = await KiotaApiCall.TryAsync<object>(async () =>
        {
            await mediaApi.Api.V1.Media[mediaFileId].DeleteAsync();
            return null;
        });
        // KiotaApiCall hard-codes OK on success; the API returns 204 NoContent for deletes.
        if (result.IsSuccess)
        {
            result = new ApiResponse<object>
            {
                StatusCode = HttpStatusCode.NoContent,
                IsSuccess = true,
                Data = null,
                RawContent = null,
            };
        }

        scenarioContext.StashLastResponse(result);
    }

    private async Task EnsureSofiaSignedInAsync()
    {
        var tokenKey = $"AccessToken:{Personas.Sofia.Email}";
        if (scenarioContext.TryGetValue(tokenKey, out string? cached) && !string.IsNullOrEmpty(cached))
        {
            apiClient.SetAuthToken(cached);
            return;
        }

        var login = await apiClient.LoginAsync(new { email = Personas.Sofia.Email, password = Personas.Sofia.Password });
        login.IsSuccess.Should().BeTrue($"Sofía login failed: {login.Error?.Detail ?? login.RawContent}");
        var jwt = login.Data.ValueKind == JsonValueKind.Object
            && login.Data.TryGetProperty("accessToken", out var access)
            ? access.GetString() ?? ""
            : "";
        jwt.Should().NotBeNullOrEmpty();
        apiClient.SetAuthToken(jwt);
        scenarioContext[tokenKey] = jwt;
    }
}
