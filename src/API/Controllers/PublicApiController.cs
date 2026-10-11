using Microsoft.AspNetCore.Mvc;

namespace Media.API.Controllers.Public;

[Route("api/v{version:apiVersion}/[controller]")]
public abstract class PublicApiController : BaseApiController;
