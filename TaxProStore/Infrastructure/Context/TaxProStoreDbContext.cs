namespace Infrastructure.Context;

using Application.Domain.Entity.Products;
using Application.Domain.Entity.Purchases;
using Application.Domain.Entity.Templates;
using Microsoft.EntityFrameworkCore;


/// <summary>
///  DbContext principal del micro-servicio «signature»
/// </summary>
public class TaxProStoreDbContext : DbContext
{
    public TaxProStoreDbContext(DbContextOptions<TaxProStoreDbContext> o)
        : base(o) { }

    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Template>(builder =>
        {
            builder.ToTable("Templates");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.HtmlContent).IsRequired().HasColumnType("varchar(max)");
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
        });
        mb.Entity<Purchase>(builder =>
        {
            builder.ToTable("Purchases");
            builder.HasKey(x => x.Id);
        
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasOne<Product>(x => x.Products)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<Product>(builder =>
        {
            builder.ToTable("Products");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).IsRequired().HasColumnType("varchar(max)");
            builder.Property(x => x.Price).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.IsActive).IsRequired();
            builder.HasOne<Template>(x => x.Templates)
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        }); 

    }
}
