using Media.Application.Services.Interfaces;
using Media.Domain.Enums.Properties;
using Media.Domain.Repositories;
using Media.Domain.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Services;
using System.Threading.Channels;

namespace Media.Infrastructure.Workers;

public sealed class ScanWorker(
    IServiceScopeFactory scopeFactory,
    Channel<MediaFileId> channel,
    TimeProvider timeProvider,
    ILogger<ScanWorker> logger) : IHostedService
{
    private Task? _backgroundTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTask = Task.Run(() => ProcessChannelAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        channel.Writer.TryComplete();
        return Task.CompletedTask;
    }

    private async Task ProcessChannelAsync(CancellationToken ct)
    {
        await foreach (var id in channel.Reader.ReadAllAsync(ct))
        {
            try
            {
                await ExecuteAsync(id, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing scan for {MediaFileId}", id);
            }
        }
    }

    private async Task ExecuteAsync(MediaFileId id, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepo>();
        var scanService = scope.ServiceProvider.GetRequiredService<IScanService>();
        var scanOrchestrator = scope.ServiceProvider.GetRequiredService<IMediaScanOrchestrator>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var deadLetterStore = scope.ServiceProvider.GetRequiredService<IDeadLetterStore>();

        var mediaFileResult = await mediaRepo.GetByIdAsync(id, ct);
        if (mediaFileResult.IsFailure)
        {
            logger.LogWarning("MediaFile {MediaFileId} not found for scanning", id);
            return;
        }

        var mediaFile = mediaFileResult.Value!;

        var beginScanResult = mediaFile.BeginScan();
        if (beginScanResult.IsFailure)
        {
            logger.LogWarning("Cannot begin scan for {MediaFileId}: {Error}", id, beginScanResult.Error?.Code.Value);
            return;
        }

        var saveResult = await mediaRepo.UpdateAsync(mediaFile, ct);
        if (saveResult.IsFailure)
        {
            logger.LogError("Failed to save scanning state for {MediaFileId}", id);
            return;
        }

        const int maxAttempts = 3;
        var delays = new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8) };

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (attempt > 0)
            {
                var incrementResult = mediaFile.IncrementScanAttempt();
                if (incrementResult.IsFailure)
                {
                    logger.LogWarning("Cannot increment scan attempt for {MediaFileId}: {Error}", id, incrementResult.Error?.Code.Value);
                    return;
                }

                await mediaRepo.UpdateAsync(mediaFile, ct);

                await Task.Delay(delays[attempt - 1], timeProvider, ct);
            }

            try
            {
                using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                scanCts.CancelAfter(TimeSpan.FromSeconds(60));

                var downloadResult = await storageService.DownloadFileAsync(mediaFile.QuarantinePath.Value, scanCts.Token);
                if (downloadResult.IsFailure)
                {
                    logger.LogWarning("Failed to download file for scanning: {MediaFileId}, attempt {Attempt}", id, attempt + 1);
                    continue;
                }

                await using var fileStream = downloadResult.Value!;
                var scanResult = await scanService.ScanAsync(fileStream, scanCts.Token);

                if (scanResult == ScanResult.Clean)
                {
                    await scanOrchestrator.ApproveAsync(id, ct);
                    return;
                }
                else
                {
                    await scanOrchestrator.RejectAsync(id, ScanRejectionReason.Infected, ct);
                    return;
                }
            }
            catch (Exception ex) when (attempt < maxAttempts - 1)
            {
                logger.LogWarning(ex, "Scan attempt {Attempt} failed for {MediaFileId}", attempt + 1, id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "All scan attempts exhausted for {MediaFileId}", id);
                await scanOrchestrator.RejectAsync(id, ScanRejectionReason.ScanFailed, ct);
                await deadLetterStore.RecordAsync(id, ex.Message, ct);
            }
        }
    }
}
