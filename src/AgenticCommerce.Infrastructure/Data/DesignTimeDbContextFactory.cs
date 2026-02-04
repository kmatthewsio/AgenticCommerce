using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgenticCommerce.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating DbContext instances for EF migrations.
/// This allows migrations to be created without needing to start the full application.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AgenticCommerceDbContext>
{
    public AgenticCommerceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AgenticCommerceDbContext>();

        // Use a default connection string for design-time operations
        // The actual connection string is provided at runtime via configuration
        var connectionString = "Host=localhost;Database=agentic;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new AgenticCommerceDbContext(optionsBuilder.Options);
    }
}
