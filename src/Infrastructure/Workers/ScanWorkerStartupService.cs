using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.ValueObjects.Ids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Media.Infrastructure.Workers;

public sealed class ScanWorkerStartupService(
    IServiceScopeFactory scopeFactory,
    Channel<MediaFileId> channel,
    ILogger<ScanWorkerStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediaUploadRepo = scope.ServiceProvider.GetRequiredService<IMediaUploadRepo>();

        var result = await mediaUploadRepo.GetByStatusesAsync(
            [MediaStatus.Pending, MediaStatus.Scanning],
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Failed to load pending/scanning media uploads on startup: {Error}", result.Error?.Code.Value);
            return;
        }

        foreach (var upload in result.Value!)
        {
            await channel.Writer.WriteAsync(upload.Id, cancellationToken);
            logger.LogInformation("Re-enqueued {MediaFileId} for scanning on startup", upload.Id);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
