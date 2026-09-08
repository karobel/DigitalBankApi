using DigitalBank.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalBank.IntegrationTests;

/// <summary>
/// Spins up the full API in-process for integration testing, replacing the
/// PostgreSQL-backed DbContext with an EF Core InMemory provider so tests
/// don't require a real database server.
/// </summary>
public class DigitalBankWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BankDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<BankDbContext>(options =>
                options.UseInMemoryDatabase($"DigitalBankTestDb_{Guid.NewGuid()}"));
        });

        builder.UseSetting("Jwt:Key", "integration-tests-secret-key-please-change-1234567890");
        builder.UseEnvironment("Development");
    }
}
