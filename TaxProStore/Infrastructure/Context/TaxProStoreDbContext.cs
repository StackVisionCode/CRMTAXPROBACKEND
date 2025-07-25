namespace Infrastructure.Context;
using Application.Domain.Entity.Purchases;
using Application.Domain.Entity.Templates;
using Domain.Entity.Form;
using Domain.Entity.Products;
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
    public DbSet<FormResponse> FormResponses => Set<FormResponse>();
    public DbSet<FormInstance> FormInstances => Set<FormInstance>();
    public DbSet<ProductFeedback> ProductFeedbacks => Set<ProductFeedback>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Template>(builder =>
        {
            builder.ToTable("Templates");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.HtmlContent).IsRequired().HasColumnType("varchar(max)");
            builder.Property(x => x.PreviewUrl).HasMaxLength(200);
            builder.Property(x => x.IsPublished).IsRequired();
            builder.Property(x => x.IsPublic).IsRequired();

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

        mb.Entity<FormResponse>(builder =>
{
    builder.ToTable("FormResponses");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.SubmittedAt).IsRequired();
    builder.Property(x => x.Data)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
    builder.HasOne(x => x.FormInstance)
            .WithMany()
            .HasForeignKey(x => x.FormInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

});

        mb.Entity<FormInstance>()
           .HasMany(fi => fi.Responses)
           .WithOne(r => r.FormInstance)
           .HasForeignKey(r => r.FormInstanceId);
        mb.Entity<FormInstance>(builder =>
        {
            builder.ToTable("FormInstances");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CustomTitle).IsRequired().HasMaxLength(200);
            builder.HasOne<Template>(x => x.Template)
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Responses)
                .WithOne(x => x.FormInstance)
                .HasForeignKey(x => x.FormInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ProductFeedback>()
            .HasIndex(p => new { p.ProductId, p.UserId })
            .IsUnique(); // Un usuario solo puede dar un feedback por producto

        mb.Entity<ProductFeedback>()
            .HasOne(p => p.Product)
            .WithMany(p => p.Feedbacks)
            .HasForeignKey(p => p.ProductId);


    }
    
    



}
