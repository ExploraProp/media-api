using Microsoft.AspNetCore.Mvc;

namespace Media.API.Controllers.Internal;

[Route("internal/v{version:apiVersion}/[controller]")]
public abstract class InternalApiController : BaseApiController;
