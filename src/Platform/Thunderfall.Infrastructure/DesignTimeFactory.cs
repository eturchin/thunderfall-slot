using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thunderfall.Infrastructure;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<GameDbContext>
{
    public GameDbContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<GameDbContext>()
                .UseNpgsql(
                    Environment.GetEnvironmentVariable("ConnectionStrings__Game")
                        ?? "Host=localhost;Database=thunderfall;Username=thunderfall"
                )
                .Options
        );
}
