using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LoanApplication.Features.ApplyForLoan;
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
        await using var context = _factory.Services.CreateAsyncScope();
        var db = context.ServiceProvider.GetRequiredService<LoanContext>();
        await db.Database.EnsureCreatedAsync();
        
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
        await using var context = _factory.Services.CreateAsyncScope();
        
        var db = context.ServiceProvider.GetRequiredService<LoanContext>();
        await db.Database.EnsureCreatedAsync();
        
        var currentTime = new DateTimeOffset(2026, 4, 5, 13, 30, 30, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.AdjustTime(currentTime);

        var client = CreateApi(fakeTimeProvider);
        using var request = CreateLoanRequest();
        await client.PostAsync("/loan-applications", request);

        db.LoanApplications.Count().ShouldBe(1);
    }

    private static StringContent CreateLoanRequest(string name = "John")
    {
        StringContent? jsonContent = null;
        try
        {
            var loanApplication = new LoanApplicationRequest(name, "john@gmail.com", 10000, 10000, 12);

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
