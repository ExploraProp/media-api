using Media.Domain.Enums.Properties;
using Media.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace Media.Infrastructure.Services;

public sealed class ScanService(ILogger<ScanService> logger) : IScanService
{
    private const int ChunkSize = 2048;

    public async Task<ScanResult> ScanAsync(Stream fileStream, CancellationToken ct)
    {
        var provider = Environment.GetEnvironmentVariable("SCAN_SERVICE_PROVIDER") ?? "clamav";

        return provider.ToLowerInvariant() switch
        {
            "clamav" => await ScanWithClamAvAsync(fileStream, ct),
            "virustotal" => throw new NotImplementedException("VirusTotal provider is not implemented"),
            _ => await ScanWithClamAvAsync(fileStream, ct),
        };
    }

    private async Task<ScanResult> ScanWithClamAvAsync(Stream fileStream, CancellationToken ct)
    {
        var host = Environment.GetEnvironmentVariable("SCAN_SERVICE_HOST") ?? "localhost";
        var port = int.TryParse(Environment.GetEnvironmentVariable("SCAN_SERVICE_PORT"), out var p) ? p : 3310;

        using var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);
        await using var networkStream = client.GetStream();

        // Send INSTREAM command
        var command = Encoding.ASCII.GetBytes("zINSTREAM\0");
        await networkStream.WriteAsync(command, ct);

        // Stream file in 2KB chunks with 4-byte big-endian size prefix
        var buffer = new byte[ChunkSize];
        var sizeBuffer = new byte[4];
        int bytesRead;

        while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, ChunkSize), ct)) > 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(sizeBuffer, (uint)bytesRead);
            await networkStream.WriteAsync(sizeBuffer, ct);
            await networkStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
        }

        // Send terminating zero chunk
        await networkStream.WriteAsync(new byte[4], ct);
        await networkStream.FlushAsync(ct);

        // Read response
        using var reader = new StreamReader(networkStream, Encoding.ASCII, leaveOpen: true);
        var response = await reader.ReadLineAsync(ct) ?? string.Empty;

        logger.LogDebug("ClamAV response: {Response}", response);

        if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
            return ScanResult.Infected;

        return ScanResult.Clean;
    }
}
