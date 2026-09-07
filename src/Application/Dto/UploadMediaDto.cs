using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Media.Application.Dto;

public sealed record UploadMediaDto(
    [Required] string OwnerType,
    [Required] string OwnerId,
    [Required] string MediaType,
    [Required] IFormFile File
);
