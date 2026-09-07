using Media.Application.Dto;
using Media.Application.Queries;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using MediatR;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Application.UseCases;

public sealed class GenerateSignedUrlUseCase(
    IMediaRepo mediaRepo,
    IStorageService storageService) : IRequestHandler<GenerateSignedUrlQuery, Result<SignedUrlDto>>
{
    public async Task<Result<SignedUrlDto>> Handle(GenerateSignedUrlQuery request, CancellationToken cancellationToken)
    {
        var mediaFileResult = await mediaRepo.GetByIdAsync(request.Id, cancellationToken);
        if (mediaFileResult.IsFailure)
            return Result<SignedUrlDto>.FromResult(mediaFileResult);

        var mediaFile = mediaFileResult.Value!;

        if (!mediaFile.IsApproved)
            return Result<SignedUrlDto>.Failure(MediaFileError.NotApproved);

        if (!mediaFile.IsPrivate)
            return Result<SignedUrlDto>.Failure(MediaFileError.Public);

        // Use the promoted original variant's path — the canonical post-approval artifact.
        // Quarantine is pre-approval storage and may be purged by a future cleanup job.
        var originalVariant = mediaFile.Variants.FirstOrDefault(v => v.VariantType == VariantType.Original);
        var storagePath = originalVariant?.Url.Value ?? mediaFile.QuarantinePath.Value;

        var signedUrlResult = await storageService.GenerateSignedUrlAsync(
            storagePath,
            TimeSpan.FromMinutes(15),
            cancellationToken);

        if (signedUrlResult.IsFailure)
            return Result<SignedUrlDto>.FromResult(signedUrlResult);

        return Result<SignedUrlDto>.Success(
            new SignedUrlDto(signedUrlResult.Value!, DateTime.UtcNow.AddMinutes(15)));
    }
}
