using Media.Application.Dto;
using Media.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Media.API.Controllers;

[ApiController]
[Route("api/v1/internal/media")]
public sealed class InternalMediaController(MediaPublicUrlService mediaPublicUrlService) : ControllerBase
{
    [HttpPost("public-urls")]
    [ProducesResponseType(typeof(MeshPublicUrlsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeshPublicUrlsResponse>> GetPublicUrls(
        [FromBody] MeshPublicUrlsRequest request,
        CancellationToken cancellationToken)
    {
        var urls = await mediaPublicUrlService.GetPublicUrlsAsync(
            request.MediaFileIds,
            cancellationToken);
        return Ok(new MeshPublicUrlsResponse(urls));
    }
}
