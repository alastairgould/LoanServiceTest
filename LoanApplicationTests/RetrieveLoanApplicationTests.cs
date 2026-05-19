using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LoanApplication.Domain;
using LoanApplication.Features.ApplyForLoan;
using LoanApplication.Features.RetrieveLoanApplication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace LoanApplicationTests;

public class RetrieveLoanApplicationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = new();

    [Fact]
    public async Task RetrieveLoanApplicationReturnsApplication_WhenItExists()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var client = CreateApi(fakeTimeProvider);
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
        details.MonthlyIncome.ShouldBe(10000);
        details.RequestedAmount.ShouldBe(10000m);
        details.TermMonths.ShouldBe(12);
        details.Status.ShouldBe(LoanStatus.Pending);
        details.CreatedAt.ShouldBe(currentTime.UtcDateTime);
        details.ReviewedAt.ShouldBeNull();
    }

    [Fact]
    public async Task RetrieveLoanApplicationReturnsNotFound_WhenItDoesNotExist()
    {
        var client = CreateApi(new FakeTimeProvider());

        var response = await client.GetAsync($"/loan-applications/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

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

    private HttpClient CreateApi(FakeTimeProvider fakeTimeProvider)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(config =>
                config.AddTransient<TimeProvider>(sp => fakeTimeProvider));

        }).CreateClient();
        return client;
    }
}
