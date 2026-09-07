using Media.Domain.Enums.Properties;
using Media.Domain.Services.Interfaces;

namespace Media.Functional.Infrastructure;

public sealed class FakeScanService : IScanService
{
    public Task<ScanResult> ScanAsync(Stream fileStream, CancellationToken ct)
        => Task.FromResult(ScanResult.Clean);
}
