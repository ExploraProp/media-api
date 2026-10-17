using Media.API.Extensions;
using Media.Application.Commands;
using Media.Application.Dto;
using Media.Application.Queries;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Media.API.Controllers.Public;

/// <summary>
/// Handles media file upload and scan-result notification.
/// </summary>
public class MediaController(IMediator mediator) : PublicApiController
{
    [HttpPost("upload")]
    [ProducesResponseType(typeof(MediaFileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var uploaderResult = HttpContext.GetRequestingClientUserId();
        if (uploaderResult.IsFailure) return uploaderResult.ToActionResult(this);

        var commandResult = UploadMediaCommand.FromDto(dto, uploaderResult.Value!);
        if (commandResult.IsFailure)
            return commandResult.ToActionResult(this);

        var result = await mediator.Send(commandResult.Value!, cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var isAdmin = HttpContext.IsAdminOrMaster();

        UploadedByClientId? requestingClientUserId = null;
        if (!isAdmin)
        {
            var requestingUserIdResult = HttpContext.GetRequestingClientUserId();
            if (requestingUserIdResult.IsFailure) return requestingUserIdResult.ToActionResult(this);
            requestingClientUserId = requestingUserIdResult.Value;
        }

        var commandResult = DeleteMediaCommand.FromId(id, requestingClientUserId, isAdmin);
        if (commandResult.IsFailure) return commandResult.ToActionResult(this);

        var result = await mediator.Send(commandResult.Value!, cancellationToken);
        return result.IsFailure ? result.ToActionResult(this) : NoContent();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MediaFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var queryResult = GetMediaFileQuery.FromId(id);
        if (queryResult.IsFailure)
            return queryResult.ToActionResult(this);

        var result = await mediator.Send(queryResult.Value!, cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(result.Value);
    }

    [HttpGet("{id}/signed-url")]
    [ProducesResponseType(typeof(SignedUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSignedUrl(string id, CancellationToken cancellationToken)
    {
        var queryResult = GenerateSignedUrlQuery.FromId(id);
        if (queryResult.IsFailure)
            return queryResult.ToActionResult(this);

        var result = await mediator.Send(queryResult.Value!, cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(result.Value);
    }

    [HttpGet("{id}/events")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvents(string id, CancellationToken cancellationToken)
    {
        var userIdResult = HttpContext.GetAuthId();
        if (userIdResult.IsFailure)
            return userIdResult.ToActionResult(this);

        var requestingUserIdResult = HttpContext.GetRequestingClientUserId();
        UploadedByClientId? clientUserId = requestingUserIdResult.IsSuccess
            ? new UploadedByClientId(requestingUserIdResult.Value!.Value)
            : null;

        var queryResult = WaitMediaFileEventQuery.FromId(
            id,
            new AuthId(userIdResult.Value!.Value),
            clientUserId);
        if (queryResult.IsFailure)
            return queryResult.ToActionResult(this);

        var result = await mediator.Send(queryResult.Value!, cancellationToken);
        return await result.ToSseEventStreamResult(this, cancellationToken);
    }
}
