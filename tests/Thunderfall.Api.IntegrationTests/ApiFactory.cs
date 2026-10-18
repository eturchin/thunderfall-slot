using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Thunderfall.Infrastructure;

namespace Thunderfall.Api.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal string TestConnection => new NpgsqlConnectionStringBuilder(_admin)
    {
        Database = _database
    }.ConnectionString;

    private readonly string _database = "tf_test_" + Guid.NewGuid().ToString("N");
    private readonly string _admin = Environment.GetEnvironmentVariable("TF_TEST_POSTGRES") ?? throw new InvalidOperationException("Set TF_TEST_POSTGRES to a dedicated test PostgreSQL connection with CREATEDB permission");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connection = new NpgsqlConnectionStringBuilder(_admin)
        {
            Database = _database
        };
        builder.UseSetting("ConnectionStrings:Game", connection.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var c = new NpgsqlConnection(_admin);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand($"CREATE DATABASE {_database}", c);
        await cmd.ExecuteNonQueryAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var c = new NpgsqlConnection(_admin);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS {_database} WITH (FORCE)", c);
        await cmd.ExecuteNonQueryAsync();
    }

    public HttpClient Client()
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Add("X-Thunderfall-Client", "web");
        return c;
    }
}
