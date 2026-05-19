using EligibilityService;
using EligibilityService.Rules;
using LoanApplication.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using LoanEntity = LoanApplication.Domain.LoanApplication;

namespace LoanApplicationTests;

public class EligibilityProcessorTests
{
    [Theory]
    [InlineData(3000, 10000, 24)]
    [InlineData(2000, 8000, 24)]
    [InlineData(3000, 12000, 24)]
    [InlineData(3000, 10000, 12)]
    [InlineData(3000, 10000, 60)]
    public async Task ApprovesLoan_WhenAllRulesPass(int monthlyIncome, int requestedAmount, int termMonths)
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var (sut, dbFactory) = CreateEligibilityService(CreateTimeProvider(currentTime));

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome, requestedAmount, termMonths);

        await sut.ProcessAsync(CancellationToken.None);

        await using var assertDb = await dbFactory.CreateDbContextAsync();
        var loan = assertDb.LoanApplications.Single(la => la.Id == loanId);
        loan.Status.ShouldBe(LoanStatus.Approved);
        loan.ReviewedAt.ShouldBe(currentTime.UtcDateTime);
    }

    [Fact]
    public async Task RejectsLoan_WhenMonthlyIncomeTooLow()
    {
        var (sut, dbFactory) = CreateEligibilityService(CreateTimeProvider());

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 1999, requestedAmount: 1000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var assertDb = await dbFactory.CreateDbContextAsync();
        assertDb.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);
    }

    [Fact]
    public async Task RejectsLoan_WhenRequestedAmountExceedsFourTimesIncome()
    {
        var (sut, dbFactory) = CreateEligibilityService(CreateTimeProvider());

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 12001m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var assertDb = await dbFactory.CreateDbContextAsync();
        assertDb.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);
    }

    [Fact]
    public async Task RejectsLoan_WhenTermBelowMinimum()
    {
        var (sut, dbFactory) = CreateEligibilityService(CreateTimeProvider());

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 5000m, termMonths: 11);

        await sut.ProcessAsync(CancellationToken.None);

        await using var assertDb = await dbFactory.CreateDbContextAsync();
        assertDb.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);
    }

    [Fact]
    public async Task RejectsLoan_WhenTermAboveMaximum()
    {
        var (sut, dbFactory) = CreateEligibilityService(CreateTimeProvider());

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 5000m, termMonths: 61);

        await sut.ProcessAsync(CancellationToken.None);

        await using var assertDb = await dbFactory.CreateDbContextAsync();
        assertDb.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);
    }

    [Fact]
    public async Task DoesNotProcessLoan_WhenAlreadyReviewed()
    {
        var (sut, dbFactory) = CreateEligibilityService(CreateTimeProvider());

        var loanId = Guid.NewGuid();
        var originalReviewedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedLoan(dbFactory, loanId, monthlyIncome: 1000, requestedAmount: 5000m, termMonths: 24,
            status: LoanStatus.Approved, reviewedAt: originalReviewedAt);

        await sut.ProcessAsync(CancellationToken.None);

        await using var assertDb = await dbFactory.CreateDbContextAsync();
        var loan = assertDb.LoanApplications.Single(la => la.Id == loanId);
        loan.Status.ShouldBe(LoanStatus.Approved);
        loan.ReviewedAt.ShouldBe(originalReviewedAt);
        assertDb.DecisionLogEntries.Count(d => d.LoanApplicationId == loanId).ShouldBe(0);
    }

    [Fact]
    public async Task LogsDecisionEntryForEachRule_WhenLoanProcessed()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var (sut, dbFactory) = CreateEligibilityService(CreateTimeProvider(currentTime));

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 10000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var assertDb = await dbFactory.CreateDbContextAsync();
        var entries = assertDb.DecisionLogEntries.Where(d => d.LoanApplicationId == loanId).ToList();
        entries.Count.ShouldBe(3);
        entries.ShouldAllBe(e => e.Passed);
        entries.ShouldAllBe(e => e.EvaluatedAt == currentTime.UtcDateTime);
    }

    private static FakeTimeProvider CreateTimeProvider(DateTimeOffset? currentTime = null)
    {
        var timeProvider = new FakeTimeProvider();
        
        if (currentTime is { } time)
        {
            timeProvider.AdjustTime(time);
        }
        
        return timeProvider;
    }

    private static async Task SeedLoan(
        IDbContextFactory<LoanContext> dbFactory,
        Guid id,
        int monthlyIncome,
        decimal requestedAmount,
        int termMonths,
        LoanStatus status = LoanStatus.Pending,
        DateTime? reviewedAt = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.LoanApplications.Add(new LoanEntity(
            id, "John", "john@gmail.com", monthlyIncome, requestedAmount, termMonths,
            status, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), reviewedAt));
        await db.SaveChangesAsync();
    }

    private static (EligibilityProcessor Sut, IDbContextFactory<LoanContext> DbFactory) CreateEligibilityService(TimeProvider timeProvider)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<LoanContext>()
            .UseSqlite(connection)
            .Options;

        using (var init = new LoanContext(options))
            init.Database.EnsureCreated();

        var dbFactory = new TestLoanContextFactory(options);
        var rules = new IEligibilityRule[]
        {
            new MinimumIncomeRule(),
            new AmountWithinLimitRule(),
            new TermWithinRangeRule()
        };
        var sut = new EligibilityProcessor(dbFactory, timeProvider, rules);
        return (sut, dbFactory);
    }

    private sealed class TestLoanContextFactory(DbContextOptions<LoanContext> options) : IDbContextFactory<LoanContext>
    {
        public LoanContext CreateDbContext() => new(options);
    }
}
