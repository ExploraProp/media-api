using Media.Application.Dto;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Kernel.Results;
using System.Text.Json;

namespace Media.API.Extensions;

public static class MediaEventStreamExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<IActionResult> ToSseEventStreamResult(
        this Result<MediaFileEventWaitDto> result,
        ControllerBase controller,
        CancellationToken cancellationToken)
    {
        if (result.IsFailure)
            return result.ToActionResult(controller);

        var response = controller.Response;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("X-Accel-Buffering", "no");

        var waitResult = result.Value!;
        if (waitResult.IsTimeout)
        {
            await response.WriteAsync("event: timeout\ndata: {}\n\n", CancellationToken.None);
        }
        else
        {
            var payload = JsonSerializer.Serialize(waitResult.Event, JsonOptions);
            await response.WriteAsync($"data: {payload}\n\n", cancellationToken);
        }

        return new EmptyResult();
    }
}
