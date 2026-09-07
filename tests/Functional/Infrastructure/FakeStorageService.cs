using Microsoft.AspNetCore.Http;
using SharedKernel.Application.Services;
using SharedKernel.Kernel.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Media.Functional.Infrastructure;

/// <summary>
/// Fake implementation of IStorageService for integration testing.
/// Returns fake URLs without calling Firebase Cloud Storage (avoids network calls in tests).
/// </summary>
public class FakeStorageService : IStorageService
{
    private const string FakeBaseUrl = "https://fake-storage.test/media";

    public Task<Result<string?>> UploadOptionalFileAsync(
        IFormFile? formFile,
        string mediaType,
        Guid ownerId)
    {
        if (formFile == null || formFile.Length == 0)
            return Task.FromResult(Result<string?>.Success(null));

        var url = BuildFakeUrl(formFile.FileName, mediaType, ownerId);
        return Task.FromResult(Result<string?>.Success(url));
    }

    public Task<Result<string>> UploadFileAsync(
        IFormFile formFile,
        string mediaType,
        Guid ownerId)
    {
        var url = BuildFakeUrl(formFile.FileName, mediaType, ownerId);
        return Task.FromResult(Result<string>.Success(url));
    }

    public Task DeleteFileAsync(string filePath) => Task.CompletedTask;

    public Task<Result<bool>> DeleteFileByUrlAsync(string fileUrl) =>
        Task.FromResult(Result<bool>.Success(true));

    public Task<Result<string>> UploadToQuarantineAsync(
        Stream fileStream,
        string fileName,
        string ownerType,
        Guid ownerId,
        Guid mediaFileId,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var path = $"quarantine/{ownerType}/{ownerId}/{mediaFileId}{extension}";
        return Task.FromResult(Result<string>.Success(path));
    }

    public Task<Result<string>> UploadStreamToPathAsync(
        Stream stream,
        string storagePath,
        string contentType,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        var url = isPublic
            ? $"https://fake-storage.test/{storagePath}"
            : storagePath;
        return Task.FromResult(Result<string>.Success(url));
    }

    public Task<Result<Stream>> DownloadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var ms = new MemoryStream();
        using var image = new Image<Rgba32>(200, 200);
        image.SaveAsJpeg(ms);
        ms.Position = 0;
        return Task.FromResult(Result<Stream>.Success((Stream)ms));
    }

    public Task<Result<string>> GenerateSignedUrlAsync(
        string storagePath,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var fakeSignedUrl = $"https://storage.googleapis.com/fake-bucket/{Uri.EscapeDataString(storagePath)}?X-Goog-Expires={(int)ttl.TotalSeconds}&X-Goog-Signature=fake";
        return Task.FromResult(Result<string>.Success(fakeSignedUrl));
    }

    private static string BuildFakeUrl(string fileName, string mediaType, Guid ownerId)
    {
        var directory = mediaType;
        var extension = Path.GetExtension(fileName);
        var unique = Guid.NewGuid().ToString("N");
        return $"{FakeBaseUrl}/{directory}/{ownerId:N}/{unique}{extension}";
    }
}
