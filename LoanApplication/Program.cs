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
