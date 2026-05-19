using System.Text.Json;
using EligibilityService;
using EligibilityService.Features.LoanEligibility;
using EligibilityService.Infrastructure.BackgroundService;
using EligibilityService.Infrastructure.Messaging;
using EligibilityService.Features.LoanEligibility.Rules;
using LoanApplication.Domain;
using LoanApplication.Domain.Events;
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
        var (sut, dbFactory) = await CreateEligibilityService(CreateTimeProvider(currentTime));

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome, requestedAmount, termMonths);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        var loan = db.LoanApplications.Single(la => la.Id == loanId);
        loan.Status.ShouldBe(LoanStatus.Approved);
        loan.ReviewedAt.ShouldBe(currentTime.UtcDateTime);
    }

    [Fact]
    public async Task RejectsLoan_WhenMonthlyIncomeTooLow()
    {
        var (sut, dbFactory) = await CreateEligibilityService();

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 1999, requestedAmount: 1000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);

        var entry = db.DecisionLogEntries.Single(d => d.LoanApplicationId == loanId && !d.Passed);
        entry.RuleName.ShouldBe("MinimumIncome");
        entry.Passed.ShouldBeFalse();
        entry.Message.ShouldBe("Monthly income must be at least £2,000");

        var passedEntries = db.DecisionLogEntries.Where(d => d.LoanApplicationId == loanId && d.Passed).ToList();
        passedEntries.Count.ShouldBe(2);
        passedEntries.ShouldAllBe(e => e.Message == "Passed Eligibility Rule");
    }

    [Fact]
    public async Task RejectsLoan_WhenRequestedAmountExceedsFourTimesIncome()
    {
        var (sut, dbFactory) = await CreateEligibilityService();

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 12001m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);

        var entry = db.DecisionLogEntries.Single(d => d.LoanApplicationId == loanId && !d.Passed);
        entry.RuleName.ShouldBe("AmountWithinLimit");
        entry.Passed.ShouldBeFalse();
        entry.Message.ShouldBe("Requested amount must be no more than monthly income multiplied by 4");
    }

    [Fact]
    public async Task RejectsLoan_WhenTermBelowMinimum()
    {
        var (sut, dbFactory) = await CreateEligibilityService();

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 5000m, termMonths: 11);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);

        var entry = db.DecisionLogEntries.Single(d => d.LoanApplicationId == loanId && !d.Passed);
        entry.RuleName.ShouldBe("TermWithinRange");
        entry.Passed.ShouldBeFalse();
        entry.Message.ShouldBe("Term must be between 12 and 60 months");
    }

    [Fact]
    public async Task RejectsLoan_WhenTermAboveMaximum()
    {
        var (sut, dbFactory) = await CreateEligibilityService();

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 5000m, termMonths: 61);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);

        var entry = db.DecisionLogEntries.Single(d => d.LoanApplicationId == loanId && !d.Passed);
        entry.RuleName.ShouldBe("TermWithinRange");
        entry.Passed.ShouldBeFalse();
        entry.Message.ShouldBe("Term must be between 12 and 60 months");
    }

    [Fact]
    public async Task DoesNotProcessLoan_WhenAlreadyReviewed()
    {
        var (sut, dbFactory) = await CreateEligibilityService();

        var loanId = Guid.NewGuid();
        var originalReviewedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedLoan(dbFactory, loanId, monthlyIncome: 1000, requestedAmount: 5000m, termMonths: 24,
            status: LoanStatus.Approved, reviewedAt: originalReviewedAt);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        var loan = db.LoanApplications.Single(la => la.Id == loanId);
        loan.Status.ShouldBe(LoanStatus.Approved);
        loan.ReviewedAt.ShouldBe(originalReviewedAt);
        db.DecisionLogEntries.Count(d => d.LoanApplicationId == loanId).ShouldBe(0);
        db.OutboxMessages.Count().ShouldBe(0);
    }

    [Fact]
    public async Task PublishesLoanApprovedEvent_WhenLoanApproved()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var (sut, dbFactory) = await CreateEligibilityService(CreateTimeProvider(currentTime));

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 10000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        var outbox = db.OutboxMessages.Single();
        outbox.Type.ShouldBe(nameof(LoanApproved));
        outbox.OccurredAt.ShouldBe(currentTime.UtcDateTime);
        outbox.PublishedAt.ShouldBeNull();

        var payload = JsonSerializer.Deserialize<LoanApproved>(outbox.Payload);
        payload.ShouldNotBeNull();
        payload.LoanApplicationId.ShouldBe(loanId);
        payload.ApprovedAt.ShouldBe(currentTime.UtcDateTime);
    }

    [Fact]
    public async Task PublishesLoanRejectedEvent_WhenLoanRejected()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var (sut, dbFactory) = await CreateEligibilityService(CreateTimeProvider(currentTime));

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 1999, requestedAmount: 1000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        var outbox = db.OutboxMessages.Single();
        outbox.Type.ShouldBe(nameof(LoanRejected));
        outbox.OccurredAt.ShouldBe(currentTime.UtcDateTime);
        outbox.PublishedAt.ShouldBeNull();

        var payload = JsonSerializer.Deserialize<LoanRejected>(outbox.Payload);
        payload.ShouldNotBeNull();
        payload.LoanApplicationId.ShouldBe(loanId);
        payload.RejectedAt.ShouldBe(currentTime.UtcDateTime);
    }

    [Fact]
    public async Task LogsDecisionEntryForEachRule_WhenLoanProcessed()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var (sut, dbFactory) = await CreateEligibilityService(CreateTimeProvider(currentTime));

        var loanId = Guid.NewGuid();
        await SeedLoan(dbFactory, loanId, monthlyIncome: 3000, requestedAmount: 10000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = await dbFactory.CreateDbContextAsync();
        var entries = db.DecisionLogEntries.Where(d => d.LoanApplicationId == loanId).ToList();
        entries.Count.ShouldBe(3);
        entries.ShouldAllBe(e => e.Passed);
        entries.ShouldAllBe(e => e.EvaluatedAt == currentTime.UtcDateTime);

        entries.ShouldAllBe(e => e.Message == "Passed Eligibility Rule");
        entries.Select(e => e.RuleName).ShouldBe(["MinimumIncome", "AmountWithinLimit", "TermWithinRange"], ignoreOrder: true);
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

    private static async Task<(EligibilityProcessor Sut, IDbContextFactory<LoanContext> DbFactory)> CreateEligibilityService(TimeProvider? timeProvider = null)
    {
        timeProvider ??= CreateTimeProvider();

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<LoanContext>()
            .UseSqlite(connection)
            .Options;

        using (var init = new LoanContext(options))
            init.Database.EnsureCreated();

        var dbFactory = new TestLoanContextFactory(options);
        var publisherFactory = new OutboxEventPublisherFactory(timeProvider);
        var rules = new IEligibilityRule[]
        {
            new MinimumIncomeRule(),
            new AmountWithinLimitRule(),
            new TermWithinRangeRule()
        };
        var processorFactory = new EligibilityProcessorFactory(dbFactory, publisherFactory, timeProvider, rules);
        var sut = await processorFactory.CreateAsync();
        return (sut, dbFactory);
    }

    private sealed class TestLoanContextFactory(DbContextOptions<LoanContext> options) : IDbContextFactory<LoanContext>
    {
        public LoanContext CreateDbContext() => new(options);
    }
}
