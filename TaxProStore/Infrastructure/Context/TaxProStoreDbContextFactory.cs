using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedLibrary;

public class TaxProStoreDbContextFactory : IDesignTimeDbContextFactory<TaxProStoreDbContext>
{
    public TaxProStoreDbContext CreateDbContext(string[] args)
    {
        var objetoConexion = new ConnectionApp();

        var optionsBuilder = new DbContextOptionsBuilder<TaxProStoreDbContext>();
        var connectionString =
            $"Server={objetoConexion.Server};Database=TaxProStoreDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new TaxProStoreDbContext(optionsBuilder.Options);
    }
}
