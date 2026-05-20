using System.Text.Json;
using EligibilityService.Features.LoanEligibility.Rules;
using EligibilityService.Infrastructure.BackgroundService;
using EligibilityService.Infrastructure.EventPublishing;
using LoanApplication.Domain;
using LoanApplication.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace LoanApplicationTests;

public class EligibilityProcessorTests
{
    private readonly EligibilityTestFixture _fixture = new();

    [Theory]
    [InlineData(3000, 10000, 24)]
    [InlineData(2000, 8000, 24)]
    [InlineData(3000, 12000, 24)]
    [InlineData(3000, 10000, 12)]
    [InlineData(3000, 10000, 60)]
    public async Task ApprovesLoan_WhenAllRulesPass(int monthlyIncome, int requestedAmount, int termMonths)
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var sut = await CreateSut(fakeTimeProvider);

        var loanId = Guid.NewGuid();
        await SeedLoan(loanId, monthlyIncome, requestedAmount, termMonths);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        var loan = db.LoanApplications.Single(la => la.Id == loanId);
        loan.Status.ShouldBe(LoanStatus.Approved);
        loan.ReviewedAt.ShouldBe(currentTime.UtcDateTime);
    }

    [Fact]
    public async Task RejectsLoan_WhenMonthlyIncomeTooLow()
    {
        var sut = await CreateSut(new FakeTimeProvider());

        var loanId = Guid.NewGuid();
        await SeedLoan(loanId, monthlyIncome: 1999, requestedAmount: 1000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
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
        var sut = await CreateSut(new FakeTimeProvider());

        var loanId = Guid.NewGuid();
        await SeedLoan(loanId, monthlyIncome: 3000, requestedAmount: 12001m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        db.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);

        var entry = db.DecisionLogEntries.Single(d => d.LoanApplicationId == loanId && !d.Passed);
        entry.RuleName.ShouldBe("AmountWithinLimit");
        entry.Passed.ShouldBeFalse();
        entry.Message.ShouldBe("Requested amount must be no more than monthly income multiplied by 4");
    }

    [Fact]
    public async Task RejectsLoan_WhenTermBelowMinimum()
    {
        var sut = await CreateSut(new FakeTimeProvider());

        var loanId = Guid.NewGuid();
        await SeedLoan(loanId, monthlyIncome: 3000, requestedAmount: 5000m, termMonths: 11);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        db.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);

        var entry = db.DecisionLogEntries.Single(d => d.LoanApplicationId == loanId && !d.Passed);
        entry.RuleName.ShouldBe("TermWithinRange");
        entry.Passed.ShouldBeFalse();
        entry.Message.ShouldBe("Term must be between 12 and 60 months");
    }

    [Fact]
    public async Task RejectsLoan_WhenTermAboveMaximum()
    {
        var sut = await CreateSut(new FakeTimeProvider());

        var loanId = Guid.NewGuid();
        await SeedLoan(loanId, monthlyIncome: 3000, requestedAmount: 5000m, termMonths: 61);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        db.LoanApplications.Single(la => la.Id == loanId).Status.ShouldBe(LoanStatus.Rejected);

        var entry = db.DecisionLogEntries.Single(d => d.LoanApplicationId == loanId && !d.Passed);
        entry.RuleName.ShouldBe("TermWithinRange");
        entry.Passed.ShouldBeFalse();
        entry.Message.ShouldBe("Term must be between 12 and 60 months");
    }

    [Fact]
    public async Task DoesNotProcessLoan_WhenAlreadyReviewed()
    {
        var sut = await CreateSut(new FakeTimeProvider());

        var loanId = Guid.NewGuid();
        var originalReviewedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedLoan(loanId, monthlyIncome: 1000, requestedAmount: 5000m, termMonths: 24,
            status: LoanStatus.Approved, reviewedAt: originalReviewedAt);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
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
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var sut = await CreateSut(fakeTimeProvider);

        var loanId = Guid.NewGuid();
        await SeedLoan(loanId, monthlyIncome: 3000, requestedAmount: 10000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        var outbox = db.OutboxMessages.Single();
        outbox.Type.ShouldBe(nameof(LoanApproved));
        outbox.OccurredAt.ShouldBe(currentTime.UtcDateTime);
        outbox.PublishedAt.ShouldBeNull();

        var payload = JsonSerializer.Deserialize<LoanApproved>(outbox.Payload);
        payload.ShouldNotBeNull();
        payload.Id.ShouldNotBe(Guid.Empty);
        payload.LoanApplicationId.ShouldBe(loanId);
        payload.ApprovedAt.ShouldBe(currentTime.UtcDateTime);
    }

    [Fact]
    public async Task PublishesLoanRejectedEvent_WhenLoanRejected()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var sut = await CreateSut(fakeTimeProvider);

        var loanId = Guid.NewGuid();
        await SeedLoan(loanId, monthlyIncome: 1999, requestedAmount: 1000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        var outbox = db.OutboxMessages.Single();
        outbox.Type.ShouldBe(nameof(LoanRejected));
        outbox.OccurredAt.ShouldBe(currentTime.UtcDateTime);
        outbox.PublishedAt.ShouldBeNull();

        var payload = JsonSerializer.Deserialize<LoanRejected>(outbox.Payload);
        payload.ShouldNotBeNull();
        payload.Id.ShouldNotBe(Guid.Empty);
        payload.LoanApplicationId.ShouldBe(loanId);
        payload.RejectedAt.ShouldBe(currentTime.UtcDateTime);
    }

    [Fact]
    public async Task LogsDecisionEntryForEachRule_WhenLoanProcessed()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var sut = await CreateSut(fakeTimeProvider);

        var loanId = Guid.NewGuid();
        await SeedLoan(loanId, monthlyIncome: 3000, requestedAmount: 10000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        var entries = db.DecisionLogEntries.Where(d => d.LoanApplicationId == loanId).ToList();
        entries.Count.ShouldBe(3);
        entries.ShouldAllBe(e => e.Passed);
        entries.ShouldAllBe(e => e.EvaluatedAt == currentTime.UtcDateTime);

        entries.ShouldAllBe(e => e.Message == "Passed Eligibility Rule");
        entries.Select(e => e.RuleName).ShouldBe(["MinimumIncome", "AmountWithinLimit", "TermWithinRange"], ignoreOrder: true);
    }

    [Fact]
    public async Task IsolatesFailure_WhenOneLoanInBatchThrows()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero));

        var firstId = Guid.NewGuid();
        var poisonId = Guid.NewGuid();
        var lastId = Guid.NewGuid();

        await SeedLoan(firstId, monthlyIncome: 3000m, requestedAmount: 5000m, termMonths: 24,
            createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedLoan(poisonId, monthlyIncome: 3000m, requestedAmount: 5000m, termMonths: 24,
            createdAt: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        await SeedLoan(lastId, monthlyIncome: 3000m, requestedAmount: 5000m, termMonths: 24,
            createdAt: new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        IEligibilityRule[] rules =
        [
            new MinimumIncomeRule(),
            new AmountWithinLimitRule(),
            new TermWithinRangeRule(),
            new ThrowingRule(poisonId)
        ];

        await using var sut = await CreateSut(fakeTimeProvider, rules: rules);
        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();

        db.LoanApplications.Single(la => la.Id == firstId).Status.ShouldBe(LoanStatus.Approved);
        db.LoanApplications.Single(la => la.Id == lastId).Status.ShouldBe(LoanStatus.Approved);

        var poison = db.LoanApplications.Single(la => la.Id == poisonId);
        poison.Status.ShouldBe(LoanStatus.Pending);
        poison.ReviewedAt.ShouldBeNull();

        db.DecisionLogEntries.Count(d => d.LoanApplicationId == poisonId).ShouldBe(0);
        db.DecisionLogEntries.Count(d => d.LoanApplicationId == firstId).ShouldBe(4);
        db.DecisionLogEntries.Count(d => d.LoanApplicationId == lastId).ShouldBe(4);

        db.OutboxMessages.Count().ShouldBe(2);
        db.OutboxMessages.ShouldAllBe(m => m.Type == nameof(LoanApproved));
    }

    [Fact]
    public async Task ProcessesUpToBatchSize_WhenMorePendingLoans()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero));

        var sut = await CreateSut(fakeTimeProvider, batchSize: 2);

        await SeedLoan(Guid.NewGuid(), monthlyIncome: 3000m, requestedAmount: 5000m, termMonths: 24);
        await SeedLoan(Guid.NewGuid(), monthlyIncome: 3000m, requestedAmount: 5000m, termMonths: 24);
        await SeedLoan(Guid.NewGuid(), monthlyIncome: 3000m, requestedAmount: 5000m, termMonths: 24);

        await sut.ProcessAsync(CancellationToken.None);

        await using var db = _fixture.DbFactory.CreateDbContext();
        db.LoanApplications.Count(la => la.Status != LoanStatus.Pending).ShouldBe(2);
        db.LoanApplications.Count(la => la.Status == LoanStatus.Pending).ShouldBe(1);
    }

    private async Task SeedLoan(
        Guid id,
        decimal monthlyIncome,
        decimal requestedAmount,
        int termMonths,
        LoanStatus status = LoanStatus.Pending,
        DateTime? reviewedAt = null,
        DateTime? createdAt = null)
    {
        await using var db = _fixture.DbFactory.CreateDbContext();
        db.LoanApplications.Add(new LoanApplication.Domain.LoanApplication(
            id, "John", "john@gmail.com", monthlyIncome, requestedAmount, termMonths,
            status, createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), reviewedAt));
        await db.SaveChangesAsync();
    }

    private async Task<EligibilityProcessor> CreateSut(
        TimeProvider timeProvider,
        int batchSize = 500,
        IEligibilityRule[]? rules = null)
    {
        rules ??=
        [
            new MinimumIncomeRule(),
            new AmountWithinLimitRule(),
            new TermWithinRangeRule()
        ];
        var processorFactory = new EligibilityProcessorFactory(
            _fixture.DbFactory, NullLoggerFactory.Instance, timeProvider, rules, batchSize);
        return await processorFactory.CreateAsync();
    }

    private sealed class ThrowingRule(Guid targetLoanId) : IEligibilityRule
    {
        public string Name => "Throwing";
        public string Message => "boom";
        public bool Evaluate(LoanApplication.Domain.LoanApplication loan)
        {
            if (loan.Id == targetLoanId)
            {
                throw new InvalidOperationException("simulated rule failure");
            }
            
            return true;
        }
    }
}
