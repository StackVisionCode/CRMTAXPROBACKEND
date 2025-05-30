using CustomerService.Domains.Customers;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }
// DbSets for each entity in the domain
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Occupation> Occupations { get; set; }
    public DbSet<ContactInfo> ContactInfos { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Dependent> Dependents { get; set; }
    public DbSet<Relationship> Relationships { get; set; }
    public DbSet<TaxInformation> TaxInformations { get; set; }
    public DbSet<FilingStatus> FilingStatuses { get; set; }
    public DbSet<MaritalStatus> MaritalStatuses { get; set; }
    public DbSet<PreferredContact> PreferredContacts { get; set; }


// Configuring the model relationships and constraints
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer → Address (uno a uno)
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.Address)
            .WithOne(a => a.Customer)
            .HasForeignKey<Address>(a => a.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        // Customer → ContactInfo (uno a uno)
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.Contact)
            .WithOne(ci => ci.Customer)
            .HasForeignKey<ContactInfo>(ci => ci.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        // Customer → Dependents (uno a muchos)
        modelBuilder.Entity<Customer>()
            .HasMany(c => c.Dependents)
            .WithOne(d => d.Customer)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        // Customer → TaxInformation (uno a uno)
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.TaxInfo)
            .WithOne(ti => ti.Customer)
            .HasForeignKey<TaxInformation>(ti => ti.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        // Customer → Occupation (muchos a uno)
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.Occupation)
            .WithMany(o => o.Customers)
            .HasForeignKey(c => c.OccupationId)
            .OnDelete(DeleteBehavior.NoAction);

        // Optional: Define required fields and constraints explicitly (optional if already using [Required] attributes)
        modelBuilder.Entity<Occupation>()
            .Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<Dependent>()
            .Property(d => d.FullName)
            .IsRequired();

        modelBuilder.Entity<Dependent>()
            .Property(d => d.DateOfBirth)
            .IsRequired();

        modelBuilder.Entity<TaxInformation>()
            .HasOne(t => t.FilingStatus)
            .WithMany(f => f.TaxInformations)
            .HasForeignKey(t => t.FilingStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Customer>()
            .HasOne(c => c.MaritalStatus)
            .WithMany(m => m.Customers)
            .HasForeignKey(c => c.MaritalStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ContactInfo>()
            .HasOne(c => c.PreferredContact)
            .WithMany(p => p.Contacts)
            .HasForeignKey(c => c.PreferredContactId)
            .OnDelete(DeleteBehavior.NoAction);


        // Configure table names and keys

        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Occupation>().ToTable("Occupations");
        modelBuilder.Entity<ContactInfo>().ToTable("ContactInfos");
        modelBuilder.Entity<Address>().ToTable("Addresses");
        modelBuilder.Entity<Dependent>().ToTable("Dependents");
        modelBuilder.Entity<ContactInfo>().HasKey(t => t.Id);
        modelBuilder.Entity<Address>().HasKey(t => t.Id);
        modelBuilder.Entity<Dependent>().HasKey(t => t.Id);
        modelBuilder.Entity<Occupation>().HasKey(t => t.Id);
        modelBuilder.Entity<Customer>().HasKey(t => t.Id);
        modelBuilder.Entity<TaxInformation>().ToTable("TaxInformations");
        modelBuilder.Entity<TaxInformation>().HasKey(t => t.Id);
        modelBuilder.Entity<Relationship>().ToTable("Relationships");
        modelBuilder.Entity<Relationship>().HasKey(r => r.Id);
        modelBuilder.Entity<FilingStatus>().ToTable("FilingStatuses");
        modelBuilder.Entity<FilingStatus>().HasKey(f => f.Id);
        modelBuilder.Entity<MaritalStatus>().ToTable("MaritalStatuses");
        modelBuilder.Entity<MaritalStatus>().HasKey(m => m.Id);
        modelBuilder.Entity<PreferredContact>().ToTable("PreferredContacts");
        modelBuilder.Entity<PreferredContact>().HasKey(p => p.Id);



        // Seed data for initial values

        modelBuilder.Entity<PreferredContact>().HasData(
    new PreferredContact { Id = Guid.Parse("40000000-0000-0000-0000-000000000001"), Name = "Email" },
    new PreferredContact { Id = Guid.Parse("40000000-0000-0000-0000-000000000002"), Name = "SMS" },
    new PreferredContact { Id = Guid.Parse("40000000-0000-0000-0000-000000000003"), Name = "Call" }
);



        modelBuilder.Entity<MaritalStatus>().HasData(
            new MaritalStatus { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), Name = "Single" },
            new MaritalStatus { Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), Name = "Married" },
            new MaritalStatus { Id = Guid.Parse("30000000-0000-0000-0000-000000000003"), Name = "Divorced" },
            new MaritalStatus { Id = Guid.Parse("30000000-0000-0000-0000-000000000004"), Name = "Widowed" }
        );


        modelBuilder.Entity<FilingStatus>().HasData(
            new FilingStatus { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "Single" },
            new FilingStatus { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Name = "MarriedJoint" },
            new FilingStatus { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Name = "MarriedSeparate" },
            new FilingStatus { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Name = "HeadOfHousehold" }
        );

        modelBuilder.Entity<Relationship>().HasData(
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Son" },
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Daughter" },
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Spouse" },
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Father" },
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Mother" },
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Name = "Brother" },
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Name = "Sister" },
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Name = "Grandparent" },
            new Relationship { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), Name = "Other" }
        );


        modelBuilder.Entity<Occupation>().HasData(
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Software Developer", Description = "Designs and develops software applications." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Accountant", Description = "Prepares and examines financial records." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Teacher", Description = "Instructs students at various educational levels." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Nurse", Description = "Provides medical care and support to patients." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), Name = "Doctor", Description = "Diagnoses and treats illnesses and injuries." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), Name = "Electrician", Description = "Installs and repairs electrical systems." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), Name = "Plumber", Description = "Maintains and repairs water systems." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), Name = "Construction Worker", Description = "Builds and repairs buildings and infrastructure." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), Name = "Police Officer", Description = "Enforces laws and protects citizens." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000010"), Name = "Firefighter", Description = "Responds to fire and rescue emergencies." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000011"), Name = "Truck Driver", Description = "Transports goods over long distances." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000012"), Name = "Chef", Description = "Prepares meals and manages kitchen staff." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000013"), Name = "Cashier", Description = "Handles customer transactions at a store." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000014"), Name = "Salesperson", Description = "Sells products or services to customers." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000015"), Name = "Security Guard", Description = "Monitors and protects property and people." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000016"), Name = "Hairdresser", Description = "Cuts, colors, and styles hair." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000017"), Name = "Mechanic", Description = "Repairs and maintains vehicles and machinery." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000018"), Name = "Janitor", Description = "Cleans and maintains buildings." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000019"), Name = "Receptionist", Description = "Manages front desk and greets visitors." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000020"), Name = "Secretary", Description = "Handles administrative and clerical tasks." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000021"), Name = "Engineer", Description = "Designs and oversees projects in various fields." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000022"), Name = "Web Developer", Description = "Builds and maintains websites and web apps." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000023"), Name = "Lawyer", Description = "Provides legal advice and representation." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000024"), Name = "Dentist", Description = "Treats issues related to teeth and oral health." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000025"), Name = "Photographer", Description = "Captures images professionally." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000026"), Name = "Uber Driver", Description = "Drives clients using the Uber app." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000027"), Name = "Lyft Driver", Description = "Drives clients using the Lyft platform." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000028"), Name = "Rideshare Driver", Description = "Provides transport via digital platforms." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000029"), Name = "Delivery Driver", Description = "Delivers food or packages locally." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000030"), Name = "Courier", Description = "Transports documents or items locally." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000031"), Name = "Freelancer", Description = "Works independently in various fields." },
         new Occupation { Id = Guid.Parse("00000000-0000-0000-0000-000000000032"), Name = "Self-Employed", Description = "Runs their own business or services." }
     );

    }


}
