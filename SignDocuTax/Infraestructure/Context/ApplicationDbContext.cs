using Domain.Documents;
using Domain.Signatures;
using Domains.Firms;
using Domains.Requirements;
using Domains.Signatures;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Context;

public class ApplicationDbContext : DbContext
{

  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
  {

  }

  public DbSet<Document> Documents { get; set; }
  public DbSet<DocumentStatus> DocumentStatus { get; set; }
  public DbSet<DocumentType> DocumentTypes { get; set; }
  public DbSet<EventSignature> EventSignatures { get; set; }

  public DbSet<Firm> Firms { get; set; }
  public DbSet<FirmStatus> FirmStatus { get; internal set; }
  public DbSet<SignatureType> SignatureTypes { get; internal set; }

  public DbSet<SignatureEventType> SignatureEventTypes { get; set; }
  public DbSet<RequirementSignature> RequirementSignatures { get; set; }
  public DbSet<StatusRequirement> StatusRequirements { get; set; }
  public DbSet<AnswerRequirement> AnswerRequirements { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<Document>().ToTable("Documents");
    modelBuilder.Entity<DocumentStatus>().ToTable("DocumentStatus");
    modelBuilder.Entity<DocumentType>().ToTable("DocumentType");
    modelBuilder.Entity<Firm>().ToTable("Firms");
    modelBuilder.Entity<FirmStatus>().ToTable("FirmStatus");
    modelBuilder.Entity<SignatureType>().ToTable("SignatureType");
    modelBuilder.Entity<EventSignature>().ToTable("EventSignatures");
    // Configure the primary key for DocumentStatus

    modelBuilder.Entity<SignatureEventType>().ToTable("SignatureEventTypes");
    modelBuilder.Entity<RequirementSignature>().ToTable("RequirementSignatures");
    modelBuilder.Entity<StatusRequirement>().ToTable("StatusRequirements");
    modelBuilder.Entity<AnswerRequirement>().ToTable("AnswerRequirements");

    modelBuilder.Entity<Document>()
    .HasOne(rp => rp.DocumentStatus)
    .WithMany(tu => tu.Documents)
    .HasForeignKey(rp => rp.DocumentStatusId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues

    modelBuilder.Entity<Document>()
    .HasOne(rp => rp.DocumentTypes)
    .WithMany(tu => tu.Documents)
    .HasForeignKey(rp => rp.DocumentTypeId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues

    modelBuilder.Entity<Firm>()
    .HasOne(f => f.FirmStatus)
    .WithMany(fs => fs.Firms)
    .HasForeignKey(f => f.FirmStatusId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues);

    modelBuilder.Entity<Firm>()
    .HasOne(f => f.SignatureType)
    .WithMany(fs => fs.Firms)
    .HasForeignKey(f => f.SignatureTypeId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues);

    modelBuilder.Entity<EventSignature>()
    .HasOne(es => es.SignatureEventType)
    .WithMany(se => se.Firms)
    .HasForeignKey(es => es.SignatureEventTypeId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues

    modelBuilder.Entity<EventSignature>()
    .HasOne(es => es.AnswerRequirement)
    .WithMany(ar => ar.Firms)
    .HasForeignKey(es => es.AnswerRequirementId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues

    modelBuilder.Entity<EventSignature>()
    .HasOne(es => es.RequirementSignature)
    .WithMany(rs => rs.Firms)
    .HasForeignKey(es => es.RequirementSignatureId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues

    modelBuilder.Entity<AnswerRequirement>()
    .HasOne(ar => ar.RequirementSignature)
    .WithMany(rs => rs.RequiredSignature)
    .HasForeignKey(ar => ar.RequirementSignatureId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues

    modelBuilder.Entity<RequirementSignature>()
    .HasOne(rs => rs.Document)
    .WithMany(d => d.RequirementSignatures)
    .HasForeignKey(rs => rs.DocumentId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues

    modelBuilder.Entity<RequirementSignature>()
    .HasOne(rs => rs.StatusRequirement)
    .WithMany(sr => sr.RequirementSignatures)
    .HasForeignKey(rs => rs.StatusSignatureId)
    .OnDelete(DeleteBehavior.NoAction); // Use Restrict to avoid cascade delete issues



  }


}