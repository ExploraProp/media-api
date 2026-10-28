using Media.Application.Dto;
using Media.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Media.API.Controllers.Internal;

/// <summary>
/// Internal ACL endpoints consumed by sibling BCs when Media runs out-of-process.
/// Documented in the OpenAPI <c>internal</c> document; consumers use
/// <c>ExploraProp.Media.Internal.ApiClient</c>.
/// </summary>
public sealed class MediaController(IMediator mediator) : InternalApiController
{
    [HttpPost("public-urls")]
    [ProducesResponseType(typeof(PublicUrlsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicUrls(
        [FromBody] PublicUrlsRequest request,
        CancellationToken cancellationToken)
    {
        var queryResult = GetPublicUrlsQuery.FromDto(request);
        if (queryResult.IsFailure) return queryResult.ToActionResult(this);

        var result = await mediator.Send(queryResult.Value!, cancellationToken);
        return result.IsFailure ? result.ToActionResult(this) : Ok(result.Value!);
    }
}
