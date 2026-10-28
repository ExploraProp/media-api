using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Media.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Consumes("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
public abstract class BaseApiController : ControllerBase
{
    protected void SetNoCacheHeaders()
    {
        Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("Expires", "0");
    }
}
