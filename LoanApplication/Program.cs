using LoanApplication.Domain;
using LoanApplication.Features.ApplyForLoan;
using LoanApplication.Features.RetrieveLoanApplication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<LoanContext>(options => options.UseSqlite("Data Source=loan-application.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplyForLoan();
builder.Services.AddRetrieveLoanApplication();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }
