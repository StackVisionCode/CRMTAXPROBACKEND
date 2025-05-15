using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Context;

public class EmailContext : DbContext
{
    public EmailContext(DbContextOptions<EmailContext> options) : base(options)
    {

    }

    //mapping table

    public DbSet<EmailMessage> EmailMessages { get; set; }
     public DbSet<EmailSettings> EmailSettings { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {


        modelBuilder.Entity<EmailMessage>().Property(e => e.To).IsRequired();
        modelBuilder.Entity<EmailMessage>().Property(e => e.Subject).IsRequired();
        modelBuilder.Entity<EmailMessage>().Property(e => e.IsHtml).IsRequired();
        modelBuilder.Entity<EmailMessage>().Property(e => e.Send).IsRequired();
       
        modelBuilder.Entity<EmailMessage>().Property(e => e.Body).IsRequired();


        modelBuilder.Entity<EmailSettings>().Property(e => e.Port).IsRequired();
        modelBuilder.Entity<EmailSettings>().Property(e => e.CompanyId).IsRequired();
        modelBuilder.Entity<EmailSettings>().Property(e => e.UserId).IsRequired();
        modelBuilder.Entity<EmailSettings>().Property(e => e.SenderEmail).IsRequired();       
        modelBuilder.Entity<EmailSettings>().Property(e => e.SmtpServer).IsRequired();
           modelBuilder.Entity<EmailSettings>().Property(e => e.Username).IsRequired();
        modelBuilder.Entity<EmailSettings>().Property(e => e.Password).IsRequired();       
        modelBuilder.Entity<EmailSettings>().Property(e => e.IsDefault).IsRequired();

        modelBuilder.Entity<EmailMessage>().ToTable("EmailMessages");
        modelBuilder.Entity<EmailMessage>().HasKey(a => a.Id);

         modelBuilder.Entity<EmailSettings>().ToTable("EmailSettings");
        modelBuilder.Entity<EmailSettings>().HasKey(a => a.Id);

        base.OnModelCreating(modelBuilder);

    }

}
