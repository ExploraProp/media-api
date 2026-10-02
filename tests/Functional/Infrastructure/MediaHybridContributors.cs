using Functional.Application;
using Functional.Application.Ports;
using Functional.Domain.Enums.Properties;
using Functional.Infrastructure;
using Media.Domain.Services.Interfaces;
using Media.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SharedKernel.Kernel.Outbox;

namespace Media.Functional.Infrastructure;

public sealed class MediaSutHostContributor : ISutHostContributor
{
    public ApplicationProgrammingInterface SutHost => ApplicationProgrammingInterface.Media;
    public ISutHost Create(WorkerOptions options, IReadOnlyDictionary<string, string> siblingBaseUrls, SiblingInfrastructure infra)
        => new MediaSutHost(options, siblingBaseUrls, infra);
}

public sealed class MediaSqlTemplateContributor : ISqlTemplateContributor
{
    public ApplicationProgrammingInterface Host => ApplicationProgrammingInterface.Media;
    public async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var opts = new DbContextOptionsBuilder<MediaDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(MediaDbContext).Assembly.FullName))
            .Options;
        await using var ctx = new MediaDbContext(opts);
        await ctx.Database.MigrateAsync(cancellationToken);
    }
}

public sealed class MediaCleanupContributor : IScenarioCleanupContributor
{
    public ApplicationProgrammingInterface Host => ApplicationProgrammingInterface.Media;
    public async Task ClearUserDataAsync(IServiceProvider sutServices, CancellationToken cancellationToken = default)
    {
        await using var scope = sutServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        await db.Set<OutboxEvent>().IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);
    }
}

file sealed class MediaSutHost(WorkerOptions options, IReadOnlyDictionary<string, string> siblingBaseUrls, SiblingInfrastructure infra)
    : HybridSutHostBase<Media.API.Program>(options, siblingBaseUrls, infra)
{
    protected override void AddConnectionStrings(Dictionary<string, string?> values)
    {
        values["ConnectionStrings:Media"] = Options.GetConnectionString(ApplicationProgrammingInterface.Media);
        values["ConnectionStrings:DefaultConnection"] = Options.GetConnectionString(ApplicationProgrammingInterface.Media);
    }

    protected override void ConfigureSutServices(IServiceCollection services)
    {
        services.RemoveAll<DbContextOptions<MediaDbContext>>();
        services.RemoveAll<MediaDbContext>();
        services.AddDbContext<MediaDbContext>((_, o) =>
            o.UseSqlServer(Options.GetConnectionString(ApplicationProgrammingInterface.Media), sql => sql.MigrationsAssembly(typeof(MediaDbContext).Assembly.FullName)));
        services.RemoveAll<IStorageService>();
        services.AddSingleton<IStorageService, FakeStorageService>();
        services.RemoveAll<IScanService>();
        services.AddSingleton<IScanService, FakeScanService>();
        services.AddHostedService(sp => new MediaMigrationService(sp));
    }
}

file sealed class MediaMigrationService(IServiceProvider sp) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = sp.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MediaDbContext>().Database.MigrateAsync(cancellationToken);
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}