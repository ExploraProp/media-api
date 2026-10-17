using Media.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Services;

namespace Media.Infrastructure.Workers;

public sealed class OrphanCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<OrphanCleanupService> logger,
    TimeSpan interval,
    int ttlHours) : IHostedService
{
    private const int MaxOrphansPerRun = 100;

    private Task? _backgroundTask;
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTask = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        if (_backgroundTask is not null)
            await Task.WhenAny(_backgroundTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await CleanupAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error during orphan cleanup");
            }
        }
    }

    internal async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediaUploadRepo = scope.ServiceProvider.GetRequiredService<IMediaUploadRepo>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var pendingBefore = DateTime.UtcNow.AddHours(-ttlHours);
        var orphansResult = await mediaUploadRepo.GetOrphansAsync(pendingBefore, MaxOrphansPerRun, ct);
        if (orphansResult.IsFailure)
        {
            logger.LogError("Failed to retrieve orphans: {Error}", orphansResult.Error?.Code.Value);
            return;
        }

        var orphans = orphansResult.Value!;
        if (orphans.Count == 0)
            return;

        var cleaned = 0;
        foreach (var upload in orphans)
        {
            var deleteReadyResult = upload.Delete();
            if (deleteReadyResult.IsFailure)
            {
                logger.LogWarning("Cannot mark orphan {MediaFileId} for deletion: {Error}",
                    upload.Id, deleteReadyResult.Error?.Code.Value);
                continue;
            }

            try
            {
                await storageService.DeleteFileAsync(upload.QuarantinePath.Value);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete quarantine blob {Path}; continuing",
                    upload.QuarantinePath.Value);
            }

            var dbDeleteResult = await mediaUploadRepo.DeleteAsync(upload.Id, ct);
            if (dbDeleteResult.IsFailure)
            {
                logger.LogError("Failed to delete orphan DB record {MediaFileId}: {Error}",
                    upload.Id, dbDeleteResult.Error?.Code.Value);
                continue;
            }

            cleaned++;
        }

        logger.LogInformation("Orphan cleanup: {Cleaned}/{Total} records removed", cleaned, orphans.Count);
    }
}
