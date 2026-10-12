using System.Net;
using ExploraProp.Media.ApiClient;
using FluentAssertions;
using Functional.Application;
using Media.Application.Commands;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Functional.StepDefinitions.MediaUpload;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using SharedKernel.ApiClient;
using SharedKernel.Kernel.Results;

namespace Media.Functional.StepDefinitions.MediaApproval;

[Binding]
[Scope(Feature = "MediaUpload")]
[Scope(Feature = "MediaApproval")]
public sealed class MediaApprovalSteps(
    ScenarioContext scenarioContext,
    MediaApiClient mediaApi,
    FeatureContext featureContext)
{
    private readonly ScenarioContext _scenarioContext = scenarioContext;
    private readonly MediaApiClient _mediaApi = mediaApi;
    private readonly Worker _worker = featureContext.Get<Worker>();

    [Given(@"the scanner approves the uploaded photo")]
    [When(@"the scanner approves the uploaded photo")]
    public async Task TheScannerApprovesTheUploadedPhoto()
    {
        var id = Guid.Parse(_scenarioContext.Get(MediaUploadKeys.MediaFileId));
        await ApproveAsync(id);
    }

    [When(@"the scanner approves media file ""(.*)""")]
    public Task TheScannerApprovesMediaFile(string mediaFileId) =>
        ApproveAsync(Guid.Parse(mediaFileId));

    [Given(@"the scanner has started scanning the uploaded photo")]
    public async Task TheScannerHasStartedScanningTheUploadedPhoto()
    {
        var mediaId = _scenarioContext.Get(MediaUploadKeys.MediaFileId);
        await using var scope = _worker.Services.CreateAsyncScope();
        var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepo>();
        var idResult = MediaFileId.Create(mediaId);
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

    [When(@"deletes the uploaded media file")]
    public async Task DeletesTheUploadedMediaFile()
    {
        var id = _scenarioContext.Get(MediaUploadKeys.MediaFileId);
        await DeleteMediaAsync(id);
    }

    [When(@"deletes media file ""(.*)""")]
    public Task DeletesMediaFile(string mediaFileId) => DeleteMediaAsync(mediaFileId);

    private async Task ApproveAsync(Guid mediaFileId)
    {
        await using var scope = _worker.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var idResult = MediaFileId.Create(mediaFileId.ToString());
        if (idResult.IsFailure)
        {
            StashFailure(idResult.Error!);
            return;
        }

        var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepo>();
        var mediaFileResult = await mediaRepo.GetByIdAsync(idResult.Value!, CancellationToken.None);
        if (mediaFileResult.IsSuccess && mediaFileResult.Value!.Status == MediaStatus.Pending)
        {
            var beginResult = mediaFileResult.Value!.BeginScan();
            if (beginResult.IsSuccess)
                await mediaRepo.UpdateAsync(mediaFileResult.Value!, CancellationToken.None);
        }

        var result = await mediator.Send(new ApproveMediaCommand(idResult.Value!), CancellationToken.None);
        if (result.IsSuccess && result.Value is not null)
        {
            var nswagDto = ToNSwagDto(result.Value);
            var response = new SwaggerResponse<MediaFileDto>(
                (int)HttpStatusCode.OK,
                new Dictionary<string, IEnumerable<string>>(),
                nswagDto);
            _scenarioContext.Put(MediaApprovalKeys.ApproveMedia, response);
            _scenarioContext.Put(MediaUploadKeys.UploadMedia, response);
            _scenarioContext.Put(MediaUploadKeys.MediaFileId, nswagDto.Id);
            _scenarioContext.Remove(MediaUploadKeys.ProblemDetails);
            return;
        }

        StashFailure(result.Error!);
    }

    private async Task DeleteMediaAsync(string mediaFileId)
    {
        try
        {
            var result = await _mediaApi.MediaDELETEAsync(mediaFileId);
            _scenarioContext.Put(MediaUploadKeys.DeleteMedia, result);
            _scenarioContext.Remove(MediaUploadKeys.ProblemDetails);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _scenarioContext.Put(MediaUploadKeys.ProblemDetails, ex.Result);
        }
    }

    private void StashFailure(Error error)
    {
        _scenarioContext.Put(
            MediaUploadKeys.ProblemDetails,
            new ProblemDetails
            {
                Status = (int)error.StatusCode,
                Code = error.Code.Value,
                Detail = error.Code.Value,
            });
    }

    private static MediaFileDto ToNSwagDto(Media.Application.Dto.MediaFileDto dto) =>
        new()
        {
            Id = dto.Id,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Status = dto.Status,
            StatusLabel = dto.StatusLabel,
            PublicUrl = dto.PublicUrl,
            OwnerType = dto.OwnerType,
            OwnerTypeLabel = dto.OwnerTypeLabel,
            MediaType = dto.MediaType,
            MediaTypeLabel = dto.MediaTypeLabel,
            UploadedAt = dto.UploadedAt,
            Variants = dto.Variants?
                .Select(v => new MediaVariantDto
                {
                    Type = v.Type,
                    TypeLabel = v.TypeLabel,
                    Url = v.Url,
                })
                .ToList() ?? [],
        };
}
