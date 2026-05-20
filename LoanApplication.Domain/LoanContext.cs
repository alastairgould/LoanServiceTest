using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Domain;

public class LoanContext(DbContextOptions<LoanContext> contextOptions) : DbContext(contextOptions)
{
   public DbSet<LoanApplication> LoanApplications { get; set; }
   public DbSet<DecisionLogEntry> DecisionLogEntries { get; set; }
   public DbSet<OutboxMessage> OutboxMessages { get; set; }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.Entity<LoanApplication>()
           .Property(e => e.Status)
           .HasConversion<string>();

       modelBuilder.Entity<LoanApplication>()
           .HasIndex(e => new { e.Status, e.CreatedAt });

       modelBuilder.Entity<DecisionLogEntry>()
           .HasOne<LoanApplication>()
           .WithMany(la => la.DecisionLogEntries)
           .HasForeignKey(e => e.LoanApplicationId);

       modelBuilder.Entity<OutboxMessage>()
           .HasIndex(e => e.OccurredAt)
           .HasFilter("PublishedAt IS NULL");
   }
}