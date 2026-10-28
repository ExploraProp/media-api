using Media.Application.Commands;
using Media.Application.Dto;
using Media.Application.Services.Interfaces;
using Media.Domain.Entities.Definitions;
using Media.Domain.Repositories;
using MediatR;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;

namespace Media.Application.UseCases;

public sealed class ApproveMediaUseCase(
    IMediaUploadRepo mediaUploadRepo,
    IMediaRepo mediaRepo,
    IMediaPolicyRepo mediaPolicyRepo,
    IMediaPromotionService mediaPromotionService,
    IStorageService storageService,
    ICatalogLabelLocalizer catalogLabels) : IRequestHandler<ApproveMediaCommand, Result<MediaFileDto>>
{
    public async Task<Result<MediaFileDto>> Handle(ApproveMediaCommand command, CancellationToken cancellationToken)
    {
        var uploadResult = await mediaUploadRepo.GetByIdAsync(command.Id, cancellationToken);
        if (uploadResult.IsFailure) return Result<MediaFileDto>.FromResult(uploadResult);
        var upload = uploadResult.Value!;

        var approveResult = upload.Approve();
        if (approveResult.IsFailure) return Result<MediaFileDto>.FromResult(approveResult);

        var policyResult = await mediaPolicyRepo.GetByOwnerTypeAsync(upload.OwnerType, cancellationToken);
        if (policyResult.IsFailure) return Result<MediaFileDto>.FromResult(policyResult);
        var policy = policyResult.Value!;

        var promotionResult = await mediaPromotionService.PromoteAsync(upload, policy, cancellationToken);
        if (promotionResult.IsFailure) return Result<MediaFileDto>.FromResult(promotionResult);
        var promotion = promotionResult.Value!;

        var accessLevel = policy.AccessLevel;
        var mediaFileResult = MediaFile.Create(upload, accessLevel, promotion.Variants);
        if (mediaFileResult.IsFailure) return Result<MediaFileDto>.FromResult(mediaFileResult);
        var mediaFile = mediaFileResult.Value!;

        var addResult = await mediaRepo.AddAsync(mediaFile, cancellationToken);
        if (addResult.IsFailure) return Result<MediaFileDto>.FromResult(addResult);

        var hardDeleteResult = await mediaUploadRepo.HardDeleteAsync(upload.Id, cancellationToken);
        if (hardDeleteResult.IsFailure) return Result<MediaFileDto>.FromResult(hardDeleteResult);

        try
        {
            await storageService.DeleteFileAsync(upload.QuarantinePath.Value);
        }
        catch
        {
            // Quarantine blob cleanup is best-effort after successful promote.
        }

        return Result<MediaFileDto>.Success(
            MediaFileDto.FromEntity(mediaFile, accessLevel, catalogLabels));
    }
}
