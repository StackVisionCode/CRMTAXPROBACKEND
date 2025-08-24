using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedLibrary;

namespace Infrastructure.Context;

public sealed class CalendarDbContextFactory : IDesignTimeDbContextFactory<CalendarDbContext>
{
    public CalendarDbContext CreateDbContext(string[] args)
    {
        // Usa tu SharedLibrary para construir el connection string
        var conn = new ConnectionApp();
        var dbName = "CalendarDB"; // si quieres, hazlo configurable por env: CALENDAR_DB_NAME
        var cs = $"Server={conn.Server};Database={dbName};User Id={conn.User};Password={conn.Password};TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<CalendarDbContext>()
            .UseSqlServer(cs, sql => sql.EnableRetryOnFailure())
            .Options;

        return new CalendarDbContext(options);
    }
}
