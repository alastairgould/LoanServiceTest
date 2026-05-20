using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LoanApplication.Domain;
using LoanApplication.Features.ApplyForLoan;
using LoanApplication.Features.RetrieveLoanApplication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace LoanApplicationTests;

public class RetrieveLoanApplicationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
    private readonly CustomWebApplicationFactory<Program> _factory = new();

    [Fact]
    public async Task RetrieveLoanApplicationReturnsApplication_WhenItExists()
    {
        var client = CreateApi();
        using var createRequest = GetLoanRequest();
        var createResponse = await client.PostAsync("/loan-applications", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<LoanApplicationResult>();

        var response = await client.GetAsync($"/loan-applications/{created!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<LoanApplicationDetails>();
        details.ShouldNotBeNull();
        details.Id.ShouldBe(created.Id);
        details.Name.ShouldBe("John");
        details.Email.ShouldBe("john@gmail.com");
        details.MonthlyIncome.ShouldBe(10000m);
        details.RequestedAmount.ShouldBe(10000m);
        details.TermMonths.ShouldBe(12);
        details.Status.ShouldBe(LoanStatus.Pending);
        details.CreatedAt.ShouldBe(CurrentTime.UtcDateTime);
        details.ReviewedAt.ShouldBeNull();
        details.DecisionLog.ShouldBeEmpty();
    }

    [Fact]
    public async Task RetrieveLoanApplicationIncludesDecisionLog_WhenEntriesExist()
    {
        var client = CreateApi();

        var loanId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var reviewedAt = new DateTime(2026, 4, 1, 0, 5, 0, DateTimeKind.Utc);

        await using (var seed = await GetDbContextFactory().CreateDbContextAsync())
        {
            seed.LoanApplications.Add(new LoanApplication.Domain.LoanApplication(
                loanId, "Jane", "jane@example.com", 3000m, 10000m, 24,
                LoanStatus.Approved, createdAt, reviewedAt));
            seed.DecisionLogEntries.AddRange(
                new DecisionLogEntry(Guid.NewGuid(), loanId, "MinimumIncome", true, "Passed Eligibility Rule", reviewedAt),
                new DecisionLogEntry(Guid.NewGuid(), loanId, "AmountWithinLimit", true, "Passed Eligibility Rule", reviewedAt),
                new DecisionLogEntry(Guid.NewGuid(), loanId, "TermWithinRange", true, "Passed Eligibility Rule", reviewedAt));
            await seed.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/loan-applications/{loanId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<LoanApplicationDetails>();
        details.ShouldNotBeNull();
        details.DecisionLog.Count.ShouldBe(3);
        details.DecisionLog.ShouldAllBe(e => e.Passed && e.Message == "Passed Eligibility Rule" && e.EvaluatedAt == reviewedAt);
        details.DecisionLog.Select(e => e.RuleName)
            .ShouldBe(["MinimumIncome", "AmountWithinLimit", "TermWithinRange"], ignoreOrder: true);
    }

    [Fact]
    public async Task RetrieveLoanApplicationReturnsNotFound_WhenItDoesNotExist()
    {
        var client = CreateApi();

        var response = await client.GetAsync($"/loan-applications/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private IDbContextFactory<LoanContext> GetDbContextFactory() =>
        _factory.Services.GetRequiredService<IDbContextFactory<LoanContext>>();

    private static StringContent GetLoanRequest(
        string name = "John",
        string email = "john@gmail.com",
        int amount = 10000,
        decimal monthlyIncome = 10000m,
        int termMonths = 12)
    {
        StringContent? jsonContent = null;
        try
        {
            var loanApplication = new LoanApplicationRequest(name, email, amount, monthlyIncome, termMonths);

            jsonContent = new(
                JsonSerializer.Serialize(loanApplication),
                Encoding.UTF8,
                "application/json");
            return jsonContent;
        }
        catch
        {
            jsonContent?.Dispose();
            throw;
        }
    }

    private HttpClient CreateApi(TimeProvider? timeProvider = null)
    {
        timeProvider ??= CreateTimeProvider();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(config =>
                config.AddTransient<TimeProvider>(sp => timeProvider));

        }).CreateClient();
        return client;
    }

    private static FakeTimeProvider CreateTimeProvider(DateTimeOffset? currentTime = null)
    {
        var provider = new FakeTimeProvider();
        provider.AdjustTime(currentTime ?? CurrentTime);
        return provider;
    }
}
