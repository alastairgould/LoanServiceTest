using LoanApplication.Features.ApplyForLoan;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/loan-applications", ([FromServices] TimeProvider timeProvider) =>
    {
        return Results.Ok(new LoanApplicationResult(Guid.NewGuid(), LoanStatus.Pending, timeProvider.GetUtcNow().UtcDateTime));
    })
    .WithName("PostLoanApplication");

app.Run();

public partial class Program { }
