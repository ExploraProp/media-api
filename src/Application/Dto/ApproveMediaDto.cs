using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Media.Application.Dto;

public sealed record ApproveMediaDto(
    [Required] IFormFile File,
    string? PublicUrl,
    [Required] string BaseStoragePath
);
