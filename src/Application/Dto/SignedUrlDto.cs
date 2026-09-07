namespace Media.Application.Dto;

public sealed record SignedUrlDto(string SignedUrl, DateTime ExpiresAt);
