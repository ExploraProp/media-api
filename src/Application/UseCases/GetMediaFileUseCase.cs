using Media.Application.Dto;
using Media.Application.Queries;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using MediatR;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;

namespace Media.Application.UseCases;

public sealed class GetMediaFileUseCase(
    IMediaRepo mediaRepo,
    IMediaUploadRepo mediaUploadRepo,
    IMediaPolicyRepo mediaPolicyRepo,
    ICatalogLabelLocalizer catalogLabels)
    : IRequestHandler<GetMediaFileQuery, Result<MediaFileDto>>
{
    public async Task<Result<MediaFileDto>> Handle(GetMediaFileQuery request, CancellationToken cancellationToken)
    {
        var readyResult = await mediaRepo.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (readyResult.IsSuccess)
        {
            var mediaFile = readyResult.Value!;
            var policyResult = await mediaPolicyRepo.GetByOwnerTypeAsync(mediaFile.OwnerType, cancellationToken);
            if (policyResult.IsFailure)
                return Result<MediaFileDto>.FromResult(policyResult);

            var accessLevel = policyResult.Value!.AccessLevel;
            if (accessLevel == AccessLevel.Private)
                return Result<MediaFileDto>.Failure(MediaPolicyError.Private);

            return Result<MediaFileDto>.Success(
                MediaFileDto.FromEntity(mediaFile, accessLevel, catalogLabels));
        }

        var uploadResult = await mediaUploadRepo.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (uploadResult.IsFailure)
            return Result<MediaFileDto>.FromResult(uploadResult);

        return Result<MediaFileDto>.Success(MediaFileDto.FromEntity(uploadResult.Value!, catalogLabels));
    }
}
