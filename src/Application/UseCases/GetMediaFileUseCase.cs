using Media.Application.Dto;
using Media.Application.Queries;
using Media.Domain.Entities.Errors;
using Media.Domain.Repositories;
using MediatR;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;

namespace Media.Application.UseCases;

public sealed class GetMediaFileUseCase(IMediaRepo mediaRepo, ICatalogLabelLocalizer catalogLabels)
    : IRequestHandler<GetMediaFileQuery, Result<MediaFileDto>>
{
    public async Task<Result<MediaFileDto>> Handle(GetMediaFileQuery request, CancellationToken cancellationToken)
    {
        var result = await mediaRepo.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (result.IsFailure)
            return Result<MediaFileDto>.FromResult(result);

        var mediaFile = result.Value!;

        if (mediaFile.IsApproved && mediaFile.IsPrivate)
            return Result<MediaFileDto>.Failure(MediaFileError.Private);

        return Result<MediaFileDto>.Success(MediaFileDto.FromEntity(mediaFile, catalogLabels));
    }
}
