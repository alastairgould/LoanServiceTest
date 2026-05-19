using System.Data.Common;
using LoanApplication.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LoanApplicationTests;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LoanContext>()
            .UseSqlite(_connection)
            .Options;
        using var context = new LoanContext(options);
        context.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(IDbContextOptionsConfiguration<LoanContext>) ||
                    d.ServiceType == typeof(DbContextOptions<LoanContext>) ||
                    d.ServiceType == typeof(IDbContextFactory<LoanContext>) ||
                    d.ServiceType == typeof(LoanContext))
                .ToList();
            
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<DbConnection>(_connection);

            services.AddDbContextFactory<LoanContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<LoanContext>>().CreateDbContext());
        });

        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
