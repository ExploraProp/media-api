using Media.Application.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Media.Infrastructure.Workers;

public sealed class DeadLetterStore(ILogger<DeadLetterStore> logger) : IDeadLetterStore
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "scan-dead-letters.jsonl");

    public async Task RecordAsync(MediaFileId id, string reason, CancellationToken ct)
    {
        try
        {
            var entry = JsonSerializer.Serialize(new { mediaFileId = id.Value, reason, failedAt = DateTime.UtcNow });
            await File.AppendAllTextAsync(FilePath, entry + "\n", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write dead-letter entry for {MediaFileId}", id);
        }
    }
}
