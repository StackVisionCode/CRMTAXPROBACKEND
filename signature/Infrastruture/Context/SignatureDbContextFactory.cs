using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedLibrary;

public class SignatureDbContextFactory : IDesignTimeDbContextFactory<SignatureDbContext>
{
    public SignatureDbContext CreateDbContext(string[] args)
    {
        var objetoConexion = new ConnectionApp();

        var optionsBuilder = new DbContextOptionsBuilder<SignatureDbContext>();
        var connectionString =
            $"Server={objetoConexion.Server};Database=SignatureDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new SignatureDbContext(optionsBuilder.Options);
    }
}
