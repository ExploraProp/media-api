using SharedKernel.Application.Constants;
using SharedKernel.Kernel.Results;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthId = Media.Domain.ValueObjects.Ids.AuthId;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;
using UploadedByClientId = Media.Domain.ValueObjects.Ids.UploadedByClientId;

namespace Media.API.Extensions;

public static class HttpContextExtensions
{
    public static JwtSecurityToken? GetJwtToken(this HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authHeader[7..].Trim();
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            return null;

        return handler.ReadJwtToken(token);
    }

    public static string? GetClaim(this HttpContext context, string claimType)
    {
        var jwtToken = context.GetJwtToken();
        return jwtToken?.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    public static Result<AuthId> GetAuthId(this HttpContext context)
    {
        var jwtToken = context.GetJwtToken();
        if (jwtToken is null)
            return Result<AuthId>.Failure(SharedFailures.Unauthorized);

        var authIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(authIdClaim) || !Guid.TryParse(authIdClaim, out var authIdGuid))
            return Result<AuthId>.Failure(SharedFailures.Unauthorized);

        return Result<AuthId>.Success(new AuthId(authIdGuid));
    }

    public static Result<UploadedByClientId> GetConsumerUserId(this HttpContext context)
    {
        var authIdResult = context.GetAuthId();
        if (authIdResult.IsFailure)
            return Result<UploadedByClientId>.FromResult(authIdResult);

        var claim = context.GetClaim(JwtClaimNames.ClientUserId);
        if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var clientUserId))
            return Result<UploadedByClientId>.Failure(SharedFailures.Unauthorized);

        return Result<UploadedByClientId>.Success(new UploadedByClientId(clientUserId));
    }

    public static Result<UploadedByClientId> GetPublisherUserId(this HttpContext context)
    {
        var authIdResult = context.GetAuthId();
        if (authIdResult.IsFailure)
            return Result<UploadedByClientId>.FromResult(authIdResult);

        var claim = context.GetClaim(JwtClaimNames.PublisherUserId);
        if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var publisherUserId))
            return Result<UploadedByClientId>.Failure(SharedFailures.Unauthorized);

        return Result<UploadedByClientId>.Success(new UploadedByClientId(publisherUserId));
    }

    public static Result<UploadedByClientId> GetRequestingClientUserId(this HttpContext context)
    {
        var consumerResult = context.GetConsumerUserId();
        if (consumerResult.IsSuccess)
            return consumerResult;

        return context.GetPublisherUserId();
    }

    public static bool IsAdminOrMaster(this HttpContext context)
    {
        var role = context.GetClaim(ClaimTypes.Role) ?? string.Empty;
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            || role.Equals("Master", StringComparison.OrdinalIgnoreCase);
    }
}
