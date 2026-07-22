using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef` tooling so migrations can be generated without booting the full
/// Api host (which would otherwise run its startup migrate/seed logic against a live database).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost;Port=3306;Database=tsthrms_dev;User=tsthrms;Password=tsthrms_dev_password;";
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0)));

        return new ApplicationDbContext(optionsBuilder.Options, new NullTenantContext(), new NullCurrentUserService());
    }

    private class NullTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool IsResolved => false;
    }

    private class NullCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
    }
}
