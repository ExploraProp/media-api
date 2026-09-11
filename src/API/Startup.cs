using Asp.Versioning;
using ExploraProp.Marketing.Internal.ApiClient;
using Media.Application.Commands;
using Media.Application.Localization;
using Media.Infrastructure.Context;
using Media.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.Application.Services;
using SharedKernel.Infrastructure.Extensions;
using SharedKernel.Infrastructure.Interceptors;
using SharedKernel.Infrastructure.Repositories;
using SharedKernel.Infrastructure.Services;
using SharedKernel.Kernel.IntegrationEvents;
using SharedKernel.Kernel.Outbox;
using SharedKernel.Kernel.Results;
using SharedKernel.Kernel.Results.Localization;

namespace Media.API;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<DomainEventDispatcherInterceptor>();
        services.AddMediaModule(Configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UploadMediaCommand).Assembly));

        var configureServices = new ConfigureServices(Configuration);
        var (storageBucket, _) = configureServices.ConfigureFirebaseStorage();
        services.AddSingleton(_ =>
        {
            var (_, json) = configureServices.ConfigureFirebaseStorage();
            return Google.Cloud.Storage.V1.StorageClient.Create(
                Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(json));
        });
        services.AddSingleton(_ =>
        {
            var (_, json) = configureServices.ConfigureFirebaseStorage();
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            return Google.Cloud.Storage.V1.UrlSigner.FromCredentialStream(stream);
        });
        services.AddScoped<IStorageService>(sp => new StorageService(
            sp.GetRequiredService<Google.Cloud.Storage.V1.StorageClient>(),
            storageBucket,
            sp.GetRequiredService<Google.Cloud.Storage.V1.UrlSigner>(),
            sp.GetRequiredService<ILogger<StorageService>>()));

        var marketingBaseUrl = Configuration["MarketingService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "MarketingService:BaseUrl is required by the extracted Media service.");
        services.AddHttpClient<IMarketingInternalClient, MarketingInternalClient>(client =>
        {
            client.BaseAddress = new Uri(marketingBaseUrl.TrimEnd('/') + "/");
        });

        services.AddScoped<IOutboxEventRepository>(sp => new OutboxEventRepository(
            sp.GetRequiredService<MediaDbContext>(),
            sp.GetRequiredService<ILogger<OutboxEventRepository>>()));
        services.AddSingleton<IOutboxDispatcher, OutboxDispatcher>();
        services.AddHostedService<OutboxProcessor>();
        services.AddSingleton<IOutboxIntegrationEventMapper, MediaOutboxIntegrationEventMapper>();
        services.Configure<IntegrationEventsOptions>(
            Configuration.GetSection(IntegrationEventsOptions.SectionName));
        if (Configuration.GetValue($"{IntegrationEventsOptions.SectionName}:UseRabbitMq", false))
            services.AddSingleton<IIntegrationEventBus, RabbitMqIntegrationEventBus>();
        else
            services.AddSingleton<IIntegrationEventBus, NoOpIntegrationEventBus>();

        services.AddHealthChecks()
            .AddDbContextCheck<MediaDbContext>("MediaDB", HealthStatus.Unhealthy, tags: ["ready"]);
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.RespectNullableAnnotations = true;
                options.JsonSerializerOptions.RespectRequiredConstructorParameters = true;
            })
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        services.AddApiDocumentation("Media API");
        services.AddProblemDetails();
        services.AddErrorLocalization(typeof(MediaError), typeof(MediaCatalogLabels), typeof(ErrorResources));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
        else app.UseHsts();
        app.UseExceptionHandler();
        app.UseRequestLocalization();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapApiDocumentation(env);
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
        });
    }
}
