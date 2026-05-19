using LoanApplication.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LoanApplicationTests;

public sealed class EligibilityTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public IDbContextFactory<LoanContext> DbFactory { get; }

    public EligibilityTestFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LoanContext>()
            .UseSqlite(_connection)
            .Options;

        using var init = new LoanContext(options);
        init.Database.EnsureCreated();

        DbFactory = new TestLoanContextFactory(options);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestLoanContextFactory(DbContextOptions<LoanContext> options) : IDbContextFactory<LoanContext>
    {
        public LoanContext CreateDbContext() => new(options);
    }
}
