using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LoanApplication.Features.ApplyForLoan;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace LoanApplicationTests;

public class LoanApplicationRequestTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = new();

    [Fact]
    public async Task LoanApplicationReturnsPending_WhenLoanApplicationPosted()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);
        
        var client = CreateApi(fakeTimeProvider);
        using var request = CreateLoanRequest();
        var response = await client.PostAsync("/loan-applications", request);
        var result = await response.Content.ReadFromJsonAsync<LoanApplicationResult>();

        result.Status.ShouldBe(LoanStatus.Pending);
        result.Id.ShouldNotBe(Guid.Empty);
        result.CreatedAt.ShouldBe(currentTime.UtcDateTime);
    }
    
    [Fact]
    public async Task LoanApplicationSaved_WhenLoanApplicationPosted()
    {
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var client = CreateApi(fakeTimeProvider);
        using var request = CreateLoanRequest();
        var response = await client.PostAsync("/loan-applications", request);
        var result = await response.Content.ReadFromJsonAsync<LoanApplicationResult>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LoanContext>();
        var saved = db.LoanApplications.Single();

        saved.Id.ShouldBe(result!.Id);
        saved.Name.ShouldBe("John");
        saved.Email.ShouldBe("john@gmail.com");
        saved.MonthlyIncome.ShouldBe(10000);
        saved.RequestedAmount.ShouldBe(10000m);
        saved.TermMonths.ShouldBe(12);
        saved.Status.ShouldBe("Pending");
        saved.CreatedAt.ShouldBe(currentTime.UtcDateTime);
        saved.ReviewedAt.ShouldBeNull();
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenNameIsEmpty()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(name: "");

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Name"].ShouldBe(["Name is required."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenNameIsWhitespace()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(name: " ");

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Name"].ShouldBe(["Name is required."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenEmailIsEmpty()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(email: "");

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Email"].ShouldBe(["A valid email is required."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenEmailIsMalformed()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(email: "not-an-email");

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Email"].ShouldBe(["A valid email is required."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenAmountIsZero()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(amount: 0);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Amount"].ShouldBe(["Amount must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenAmountIsNegative()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(amount: -1);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Amount"].ShouldBe(["Amount must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenMonthlyIncomeIsZero()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(monthlyIncome: 0m);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["MonthlyIncome"].ShouldBe(["Monthly income must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenMonthlyIncomeIsNegative()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(monthlyIncome: -1m);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["MonthlyIncome"].ShouldBe(["Monthly income must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenTermMonthsIsZero()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(termMonths: 0);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["TermMonths"].ShouldBe(["Term months must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenTermMonthsIsNegative()
    {
        var client = CreateApi(new FakeTimeProvider());
        using var request = CreateLoanRequest(termMonths: -1);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["TermMonths"].ShouldBe(["Term months must be greater than zero."]);
    }

    private static StringContent CreateLoanRequest(
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
