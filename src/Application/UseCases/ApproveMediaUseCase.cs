using Media.Application.Commands;
using Media.Application.Dto;
using Media.Application.Services.Interfaces;
using Media.Domain.Repositories;
using MediatR;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;

namespace Media.Application.UseCases;

public sealed class ApproveMediaUseCase(
    IMediaRepo mediaRepo,
    IMediaPolicyRepo mediaPolicyRepo,
    IMediaPromotionService mediaPromotionService,
    ICatalogLabelLocalizer catalogLabels) : IRequestHandler<ApproveMediaCommand, Result<MediaFileDto>>
{
    public async Task<Result<MediaFileDto>> Handle(ApproveMediaCommand command, CancellationToken cancellationToken)
    {
        var mediaFileResult = await mediaRepo.GetByIdAsync(command.Id, cancellationToken);
        if (mediaFileResult.IsFailure) return Result<MediaFileDto>.FromResult(mediaFileResult);
        var mediaFile = mediaFileResult.Value!;

        var policyResult = await mediaPolicyRepo.GetByOwnerTypeAsync(mediaFile.OwnerType, cancellationToken);
        if (policyResult.IsFailure) return Result<MediaFileDto>.FromResult(policyResult);
        var policy = policyResult.Value!;

        var promotionResult = await mediaPromotionService.PromoteAsync(mediaFile, policy, cancellationToken);
        if (promotionResult.IsFailure) return Result<MediaFileDto>.FromResult(promotionResult);
        var promotion = promotionResult.Value!;

        var publicUrl = promotion.IsPublic ? promotion.PromotedUrl : null;
        var approveResult = mediaFile.Approve(publicUrl, policy.AccessLevel, promotion.Variants);
        if (approveResult.IsFailure) return Result<MediaFileDto>.FromResult(approveResult);

        var saveResult = await mediaRepo.UpdateAsync(mediaFile, cancellationToken);
        if (saveResult.IsFailure) return Result<MediaFileDto>.FromResult(saveResult);

        return Result<MediaFileDto>.Success(MediaFileDto.FromEntity(mediaFile, catalogLabels));
    }
}
