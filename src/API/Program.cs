using Media.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Media.API;

public class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        if (Environment.GetEnvironmentVariable("APPLY_MIGRATIONS") == "true")
        {
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<MediaDbContext>().Database.Migrate();
        }
        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
