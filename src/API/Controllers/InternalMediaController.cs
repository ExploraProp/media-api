using Media.Application.Dto;
using Media.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Media.API.Controllers;

[ApiController]
[Route("api/v1/internal/media")]
public sealed class InternalMediaController(MediaPublicUrlService mediaPublicUrlService) : ControllerBase
{
    [HttpPost("public-urls")]
    [ProducesResponseType(typeof(PublicUrlsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicUrlsResponse>> GetPublicUrls(
        [FromBody] PublicUrlsRequest request,
        CancellationToken cancellationToken)
    {
        var urls = await mediaPublicUrlService.GetPublicUrlsAsync(
            request.MediaFileIds,
            cancellationToken);
        return Ok(new PublicUrlsResponse(urls));
    }
}
