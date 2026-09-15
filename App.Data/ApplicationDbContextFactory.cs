using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace App.Data;

/// <summary>
/// Lets EF Core create migrations without starting the web host. Runtime
/// connection settings still live in App.Web/appsettings.json.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DesignTimeConnection =
        "Server=.\\SQLEXPRESS;Database=TimeTracker;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DesignTimeConnection)
            .Options;

        return new ApplicationDbContext(options);
    }
}
