using LoanApplication.Features.ApplyForLoan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<LoanContext>(options => options.UseSqlite("Data Source=loan-application.db"));
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/loan-applications", (
        [FromBody] LoanApplicationRequest request,
        [FromServices] LoanContext loanContext,
        [FromServices] TimeProvider timeProvider) =>
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
            errors[nameof(request.Name)] = ["Name is required."];
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            errors[nameof(request.Email)] = ["A valid email is required."];
        if (request.Amount <= 0)
            errors[nameof(request.Amount)] = ["Amount must be greater than zero."];
        if (request.MonthlyIncome <= 0)
            errors[nameof(request.MonthlyIncome)] = ["Monthly income must be greater than zero."];
        if (request.TermMonths <= 0)
            errors[nameof(request.TermMonths)] = ["Term months must be greater than zero."];

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var id = Guid.NewGuid();
        var createdAt = timeProvider.GetUtcNow().UtcDateTime;

        loanContext.LoanApplications.Add(new LoanApplication.Features.ApplyForLoan.LoanApplication(
            id,
            request.Name,
            request.Email,
            (int)request.MonthlyIncome,
            request.Amount,
            request.TermMonths,
            "Pending",
            createdAt,
            null));

        loanContext.SaveChanges();
        return Results.Ok(new LoanApplicationResult(id, LoanStatus.Pending, createdAt));
    })
    .WithName("PostLoanApplication");

app.Run();

public partial class Program { }
