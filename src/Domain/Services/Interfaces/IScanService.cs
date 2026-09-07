using Media.Domain.Enums.Properties;

namespace Media.Domain.Services.Interfaces;

public interface IScanService
{
    Task<ScanResult> ScanAsync(Stream fileStream, CancellationToken ct);
}
