using EligibilityService;
using LoanApplication.Domain;
using LoanApplication.Features.ApplyForLoan;
using LoanApplication.Features.RetrieveLoanApplication;
using Microsoft.EntityFrameworkCore;
using OutboxPublisherService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContextFactory<LoanContext>(options => options.UseSqlite("Data Source=loan-application.db"));
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<LoanContext>>().CreateDbContext());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplyForLoan();
builder.Services.AddRetrieveLoanApplication();
builder.Services.AddEligibilityService();
builder.Services.AddOutboxPublisherService();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<LoanContext>();
    ctx.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }
