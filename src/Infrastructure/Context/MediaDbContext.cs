using Media.Domain.Entities.Definitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SharedKernel.Infrastructure.Context;

namespace Media.Infrastructure.Context;

public class MediaDbContext(DbContextOptions<MediaDbContext> options) : ModuleDbContext(options)
{
    public DbSet<MediaFile> MediaFile { get; set; } = null!;
    public DbSet<MediaPolicy> MediaPolicy { get; set; } = null!;
    public DbSet<MediaVariant> MediaVariant { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public class MediaDbContextFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    public MediaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Media")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'Media' (or fallback 'DefaultConnection') not found.");

        var optionsBuilder = new DbContextOptionsBuilder<MediaDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            b => b.MigrationsAssembly(typeof(MediaDbContext).Assembly.FullName));

        return new MediaDbContext(optionsBuilder.Options);
    }
}
