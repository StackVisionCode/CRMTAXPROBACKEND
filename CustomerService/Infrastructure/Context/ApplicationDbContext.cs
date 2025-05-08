using CustomerService.Domains.Customers;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerType> CustomerTypes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Customer>().HasKey(t => t.Id);
        modelBuilder.Entity<CustomerType>().ToTable("CustomerTypes");
        modelBuilder.Entity<CustomerType>().HasKey(t => t.Id);

        // relación entre Customer y CustomerType
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.CustomerType)
            .WithMany(ct => ct.Customers)
            .HasForeignKey(c => c.CustomerTypeId);

        // todo CustomerType data default
        modelBuilder.Entity<CustomerType>().HasData(
            new CustomerType
            {
                Id = 1,
                Name = "Individual",
                Description = "Individual customer type"
            },
            new CustomerType
            {
                Id = 2,
                Name = "Business",
                Description = "Business customer type"
            }
        );

        // todo Customer data default
        modelBuilder.Entity<Customer>().HasData(
          new Customer
          {
              Id = 1,
              CompanyId = 1,
              TaxUserId = 1,
              ContactId = 1,
              TeamMemberId = 1,
              Name = "Carlos",
              LastName = "Castillo",
              SSN = "123456789",
              Email = "castillox671@gmail.com",
              CustomerTypeId = 1,
        });
  }
}
