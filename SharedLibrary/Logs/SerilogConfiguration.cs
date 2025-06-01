using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace SharedLibrary.Logs;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(IConfiguration configuration)
    {
        var logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "LogsApplication");

        if (!Directory.Exists(logFolderPath))
        {
            Directory.CreateDirectory(logFolderPath);
        }

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.File(
                Path.Combine(logFolderPath, "LogsApplication-.txt"),
                rollingInterval: RollingInterval.Day
            )
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .Enrich.FromLogContext()
            .CreateLogger();
    }
}
