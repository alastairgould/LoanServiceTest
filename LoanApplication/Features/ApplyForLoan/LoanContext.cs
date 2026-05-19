using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Features.ApplyForLoan;

public class LoanContext : DbContext
{
   public DbSet<LoanApplication> LoanApplications { get; set; }
   public DbSet<DecisionLogEntry> DecisionLogEntries { get; set; }
   
   public string DbPath { get; }

   public LoanContext(DbContextOptions<LoanContext> contextOptions) : base(contextOptions)
   {
       var folder = Environment.SpecialFolder.LocalApplicationData;
       var path = Environment.GetFolderPath(folder);
       DbPath = System.IO.Path.Join(path, "loans.db");
   }
}