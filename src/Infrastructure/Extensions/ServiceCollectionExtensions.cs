using Media.Application.Services;
using Media.Application.Services.Interfaces;
using Media.Domain.Repositories;
using Media.Domain.Services.Interfaces;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Repositories;
using Media.Infrastructure.Services;
using Media.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SharedKernel.Infrastructure.Interceptors;
using System.Threading.Channels;

namespace Media.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediaModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ValidateWatermarkPath();
        var connectionString = configuration.GetConnectionString("Media")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'Media' (or fallback 'DefaultConnection') not found.");

        services.AddDbContext<Context.MediaDbContext>((sp, options) =>
            options
                .UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(Context.MediaDbContext).Assembly.FullName))
                .AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>()));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IMediaRepo, MediaRepo>();
        services.AddScoped<IMediaPolicyRepo, MediaPolicyRepo>();
        services.AddScoped<IContentTypeDetectionService, ContentTypeDetectionService>();
        services.AddScoped<IMediaUploadFileService, MediaUploadFileService>();
        services.AddScoped<IMediaPromotionService, MediaPromotionService>();
        services.AddScoped<IMediaScanOrchestrator, MediaScanOrchestrator>();
        services.AddScoped<IImageProcessingService, ImageProcessingService>();
        services.AddScoped<IMediaEventAccessService, MediaEventAccessService>();
        services.AddScoped<IMediaPublicUrlService, MediaPublicUrlService>();
        services.AddSingleton<IMediaEventChannel, MediaEventChannel>();
        services.AddSingleton<IMediaEventWaitService, MediaEventWaitService>();
        services.AddSingleton(_ => Channel.CreateUnbounded<MediaFileId>(new UnboundedChannelOptions { SingleReader = true }));
        services.AddScoped<IScanService, ScanService>();
        services.AddScoped<IDeadLetterStore, DeadLetterStore>();
        services.AddHostedService<ScanWorkerStartupService>();
        services.AddHostedService<ScanWorker>();
        services.AddHostedService(sp => new OrphanCleanupService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<OrphanCleanupService>>(),
            TimeSpan.FromMinutes(int.TryParse(Environment.GetEnvironmentVariable("MEDIA_ORPHAN_CLEANUP_INTERVAL_MINUTES"), out var m) && m > 0 ? m : 60),
            int.TryParse(Environment.GetEnvironmentVariable("MEDIA_ORPHAN_TTL_HOURS"), out var h) && h > 0 ? h : 24));
        return services;
    }

    private static void ValidateWatermarkPath()
    {
        var watermarkPath = Environment.GetEnvironmentVariable("MEDIA_WATERMARK_PATH");
        if (string.IsNullOrWhiteSpace(watermarkPath))
            throw new InvalidOperationException(
                "Watermark is not configured. Set MEDIA_WATERMARK_PATH in environment or configuration.");
        if (!File.Exists(watermarkPath))
            throw new InvalidOperationException(
                $"Watermark file not found at '{watermarkPath}'. Set MEDIA_WATERMARK_PATH to a valid file path.");
    }
}
