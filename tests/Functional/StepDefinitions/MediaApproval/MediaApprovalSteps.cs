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
    private static readonly Guid UnknownId = Guid.Empty;
    private static readonly string UnknownIdText = UnknownId.ToString();

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

    [When(@"the scanner approves an unknown media file")]
    public Task TheScannerApprovesAnUnknownMediaFile() =>
        ApproveAsync(UnknownId);

    [Given(@"the scanner has started scanning the uploaded photo")]
    public async Task TheScannerHasStartedScanningTheUploadedPhoto()
    {
        var mediaId = _scenarioContext.Get(MediaUploadKeys.MediaFileId);
        await using var scope = _worker.Services.CreateAsyncScope();
        var mediaUploadRepo = scope.ServiceProvider.GetRequiredService<IMediaUploadRepo>();
        var idResult = MediaFileId.Create(mediaId);
        idResult.IsSuccess.Should().BeTrue();
        var fileResult = await mediaUploadRepo.GetByIdAsync(idResult.Value!, CancellationToken.None);
        fileResult.IsSuccess.Should().BeTrue();
        var file = fileResult.Value!;
        if (file.Status == MediaStatus.Pending)
        {
            file.BeginScan();
            await mediaUploadRepo.UpdateAsync(file, CancellationToken.None);
        }
    }

    [When(@"deletes the uploaded media file")]
    public async Task DeletesTheUploadedMediaFile()
    {
        var id = _scenarioContext.Get(MediaUploadKeys.MediaFileId);
        await DeleteMediaAsync(id);
    }

    [When(@"deletes an unknown media file")]
    public Task DeletesAnUnknownMediaFile() => DeleteMediaAsync(UnknownIdText);

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

        var mediaUploadRepo = scope.ServiceProvider.GetRequiredService<IMediaUploadRepo>();
        var uploadResult = await mediaUploadRepo.GetByIdAsync(idResult.Value!, CancellationToken.None);
        if (uploadResult.IsSuccess && uploadResult.Value!.Status == MediaStatus.Pending)
        {
            var beginResult = uploadResult.Value!.BeginScan();
            if (beginResult.IsSuccess)
                await mediaUploadRepo.UpdateAsync(uploadResult.Value!, CancellationToken.None);
        }

        var result = await mediator.Send(new ApproveMediaCommand(idResult.Value!), CancellationToken.None);
        if (result.IsSuccess && result.Value is not null)
        {
            var clientDto = ToClientDto(result.Value);
            var response = new SwaggerResponse<MediaFileDto>(
                (int)HttpStatusCode.OK,
                new Dictionary<string, IEnumerable<string>>(),
                clientDto);
            _scenarioContext.Put(MediaApprovalKeys.ApproveMedia, response);
            _scenarioContext.Put(MediaUploadKeys.UploadMedia, response);
            _scenarioContext.Put(MediaUploadKeys.MediaFileId, clientDto.Id);
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

    private static MediaFileDto ToClientDto(Media.Application.Dto.MediaFileDto dto) =>
        new()
        {
            Id = dto.Id,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            // Local generated client still uses Status/StatusLabel until OpenAPI regen;
            // Application DTO already nests CatalogLabelDto.
            Status = dto.Status.I18nCode,
            StatusLabel = dto.Status.Label,
            PublicUrl = dto.PublicUrl,
            OwnerType = dto.OwnerType.I18nCode,
            OwnerTypeLabel = dto.OwnerType.Label,
            UploadedAt = dto.UploadedAt,
            Variants = dto.Variants?
                .Select(v => new MediaVariantDto
                {
                    Type = v.Type.I18nCode,
                    TypeLabel = v.Type.Label,
                    Url = v.Url,
                    Metadata = v.Metadata is null
                        ? null
                        : new MediaVariantMetadataDto
                        {
                            Width = v.Metadata.Width,
                            Height = v.Metadata.Height,
                        },
                    FileSizeBytes = v.FileSizeBytes,
                })
                .ToList() ?? [],
        };
}
