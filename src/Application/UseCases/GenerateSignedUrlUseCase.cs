using Media.Application.Dto;
using Media.Application.Queries;
using Media.Domain.Entities.Definitions;
using Media.Domain.Entities.Errors;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using MediatR;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;

namespace Media.Application.UseCases;

public sealed class GenerateSignedUrlUseCase(
    IMediaRepo mediaRepo,
    IMediaPolicyRepo mediaPolicyRepo,
    IStorageService storageService) : IRequestHandler<GenerateSignedUrlQuery, Result<SignedUrlDto>>
{
    public async Task<Result<SignedUrlDto>> Handle(GenerateSignedUrlQuery request, CancellationToken cancellationToken)
    {
        var mediaFileResult = await mediaRepo.GetByIdAsync(request.Id, cancellationToken);
        if (mediaFileResult.IsFailure)
            return Result<SignedUrlDto>.Failure(SharedFailures.NotFound(nameof(MediaFile)));

        var mediaFile = mediaFileResult.Value!;

        var policyResult = await mediaPolicyRepo.GetByOwnerTypeAsync(mediaFile.OwnerType, cancellationToken);
        if (policyResult.IsFailure)
            return Result<SignedUrlDto>.FromResult(policyResult);

        var policy = policyResult.Value!;
        if (policy.AccessLevel != AccessLevel.Private)
            return Result<SignedUrlDto>.Failure(MediaPolicyError.Public);

        var originalVariant = mediaFile.OriginalVariant;
        if (originalVariant is null)
            return Result<SignedUrlDto>.Failure(SharedFailures.NotFound(nameof(MediaFile)));

        var signedUrlResult = await storageService.GenerateSignedUrlAsync(
            originalVariant.Url.Value,
            TimeSpan.FromMinutes(15),
            cancellationToken);

        if (signedUrlResult.IsFailure)
            return Result<SignedUrlDto>.FromResult(signedUrlResult);

        return Result<SignedUrlDto>.Success(
            new SignedUrlDto(signedUrlResult.Value!, DateTime.UtcNow.AddMinutes(15)));
    }
}
