using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Domain;

public class LoanContext : DbContext
{
   public DbSet<LoanApplication> LoanApplications { get; set; }
   public DbSet<DecisionLogEntry> DecisionLogEntries { get; set; }
   public DbSet<OutboxMessage> OutboxMessages { get; set; }
   
   public string DbPath { get; }

   public LoanContext(DbContextOptions<LoanContext> contextOptions) : base(contextOptions)
   {
       var folder = Environment.SpecialFolder.LocalApplicationData;
       var path = Environment.GetFolderPath(folder);
       DbPath = System.IO.Path.Join(path, "loans.db");
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.Entity<LoanApplication>()
           .Property(e => e.Status)
           .HasConversion<string>();

       modelBuilder.Entity<LoanApplication>()
           .HasIndex(e => e.Status);

       modelBuilder.Entity<DecisionLogEntry>()
           .HasOne<LoanApplication>()
           .WithMany()
           .HasForeignKey(e => e.LoanApplicationId);

       modelBuilder.Entity<OutboxMessage>()
           .HasIndex(e => e.OccurredAt)
           .HasFilter("PublishedAt IS NULL");
   }
}