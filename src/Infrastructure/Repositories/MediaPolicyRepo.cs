using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Filters;
using SharedKernel.Infrastructure.Repositories.Base;
using SharedKernel.Kernel.Results;
using System.Linq.Expressions;
namespace Media.Infrastructure.Repositories;

public sealed class MediaPolicyRepo(MediaDbContext context, ILogger<MediaPolicyRepo> logger)
    : BaseRepo<MediaPolicy, MediaPolicyId>(context, logger), IMediaPolicyRepo
{
    public async Task<Result<MediaPolicy>> GetByOwnerTypeAsync(OwnerType ownerType, CancellationToken ct)
    {
        try
        {
            var policy = await Set.FirstOrDefaultAsync(p => p.OwnerType == ownerType, ct);
            return policy is null
                ? Result<MediaPolicy>.Failure(SharedFailures.NotFound(nameof(MediaPolicy)))
                : Result<MediaPolicy>.Success(policy);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting media policy by owner type: {OwnerType}", ownerType);
            return Result<MediaPolicy>.Failure(SharedFailures.SystemFailed);
        }
    }

    protected override Result<Expression<Func<MediaPolicy, object>>> GetSortProperty(string propertyName) =>
        Result<Expression<Func<MediaPolicy, object>>>.Success(p => p.OwnerType);

    protected override Result<Expression<Func<MediaPolicy, bool>>> GetFilterProperty(
        string propertyName,
        FilterOperator @operator,
        string value) =>
        Result<Expression<Func<MediaPolicy, bool>>>.Success(p => p.OwnerType.ToString() == value);
}
