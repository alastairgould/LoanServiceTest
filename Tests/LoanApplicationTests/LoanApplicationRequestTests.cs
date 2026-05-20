using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LoanApplication.Domain;
using LoanApplication.Features.ApplyForLoan;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace LoanApplicationTests;

public class LoanApplicationRequestTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
    private readonly CustomWebApplicationFactory<Program> _factory = new();

    [Fact]
    public async Task LoanApplicationReturnsPending_WhenLoanApplicationPosted()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest();
        var response = await client.PostAsync("/loan-applications", request);
        var result = await response.Content.ReadFromJsonAsync<LoanApplicationResult>();

        result.Status.ShouldBe(LoanStatus.Pending);
        result.Id.ShouldNotBe(Guid.Empty);
        result.CreatedAt.ShouldBe(CurrentTime.UtcDateTime);
    }

    [Fact]
    public async Task LoanApplicationSaved_WhenLoanApplicationPosted()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest();
        var response = await client.PostAsync("/loan-applications", request);
        var result = await response.Content.ReadFromJsonAsync<LoanApplicationResult>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LoanContext>();
        var saved = db.LoanApplications.Single();

        saved.Id.ShouldBe(result!.Id);
        saved.Name.ShouldBe("John");
        saved.Email.ShouldBe("john@gmail.com");
        saved.MonthlyIncome.ShouldBe(10000m);
        saved.RequestedAmount.ShouldBe(10000m);
        saved.TermMonths.ShouldBe(12);
        saved.Status.ShouldBe(LoanStatus.Pending);
        saved.CreatedAt.ShouldBe(CurrentTime.UtcDateTime);
        saved.ReviewedAt.ShouldBeNull();
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenNameIsEmpty()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(name: "");

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Name"].ShouldBe(["Name is required."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenNameIsWhitespace()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(name: " ");

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Name"].ShouldBe(["Name is required."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenEmailIsEmpty()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(email: "");

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Email"].ShouldBe(["A valid email is required."]);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@gmail.com")]
    [InlineData("john@")]
    [InlineData("john@@gmail.com")]
    public async Task LoanApplicationReturnsValidationError_WhenEmailIsMalformed(string email)
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(email: email);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["Email"].ShouldBe(["A valid email is required."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenRequestedAmountIsZero()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(requestedAmount: 0m);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["RequestedAmount"].ShouldBe(["Requested amount must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenRequestedAmountIsNegative()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(requestedAmount: -1m);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["RequestedAmount"].ShouldBe(["Requested amount must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenMonthlyIncomeIsZero()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(monthlyIncome: 0m);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["MonthlyIncome"].ShouldBe(["Monthly income must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenMonthlyIncomeIsNegative()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(monthlyIncome: -1m);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["MonthlyIncome"].ShouldBe(["Monthly income must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenTermMonthsIsZero()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(termMonths: 0);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["TermMonths"].ShouldBe(["Term months must be greater than zero."]);
    }

    [Fact]
    public async Task LoanApplicationReturnsValidationError_WhenTermMonthsIsNegative()
    {
        var client = CreateApi();
        using var request = CreateLoanRequest(termMonths: -1);

        var response = await client.PostAsync("/loan-applications", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem!.Errors["TermMonths"].ShouldBe(["Term months must be greater than zero."]);
    }

    private static StringContent CreateLoanRequest(
        string name = "John",
        string email = "john@gmail.com",
        decimal monthlyIncome = 10000m,
        decimal requestedAmount = 10000m,
        int termMonths = 12)
    {
        var loanApplication = new LoanApplicationRequest(name, email, monthlyIncome, requestedAmount, termMonths);
        return new StringContent(JsonSerializer.Serialize(loanApplication), Encoding.UTF8, "application/json");
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
