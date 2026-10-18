using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Thunderfall.Application.Contracts;
using Thunderfall.Application.Services;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Infrastructure;

namespace Thunderfall.Api.IntegrationTests;

public sealed class HistoryQueryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task HistoryQueriesDoNotLoadReplayPayloads()
    {
        var (session, secret) = await CreateSessionAsync();
        await ExecuteRoundAsync(session.Id, secret);
        var observer = new HistoryQueryObserver();
        using var scope = factory.Services.CreateScope();
        var service = CreateService(scope.ServiceProvider, observer);

        var page = await service.HistoryPageAsync(session.Id, secret, 1, 20, default);
        var legacy = await service.HistoryAsync(session.Id, secret, 0, 20, default);

        Assert.Single(page.Items);
        Assert.Single(legacy);
        Assert.NotEmpty(observer.Commands);
        Assert.All(observer.Commands, command => Assert.DoesNotContain("OutcomeJson", command));
    }

    [Fact]
    public async Task PageAndTotalShareSnapshotWhenRoundCommitsBetweenQueries()
    {
        var (session, secret) = await CreateSessionAsync();
        await ExecuteRoundAsync(session.Id, secret);
        var observer = new HistoryQueryObserver(() => ExecuteRoundAsync(session.Id, secret));
        using var scope = factory.Services.CreateScope();
        var service = CreateService(scope.ServiceProvider, observer);

        var page = await service.HistoryPageAsync(session.Id, secret, 1, 20, default);

        Assert.True(observer.Interleaved);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Total);
        var nextPage = await service.HistoryPageAsync(session.Id, secret, 1, 20, default);
        Assert.Equal(2, nextPage.Total);
        Assert.Equal(2, nextPage.Items.Count);
        var beyondEnd = await service.HistoryPageAsync(session.Id, secret, 2, 20, default);
        Assert.Empty(beyondEnd.Items);
        Assert.Equal(2, beyondEnd.Total);
    }

    private SessionService CreateService(IServiceProvider services, HistoryQueryObserver observer)
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseNpgsql(factory.TestConnection)
            .AddInterceptors(observer)
            .Options;
        return new SessionService(
            new PostgresSessionStore(new ObservedContextFactory(options)),
            services.GetRequiredService<IGameRegistry>()
        );
    }

    private async Task<(SessionView View, string Secret)> CreateSessionAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await scope
            .ServiceProvider.GetRequiredService<SessionService>()
            .CreateAsync("classic", default);
    }

    private async Task ExecuteRoundAsync(Guid sessionId, string secret)
    {
        using var scope = factory.Services.CreateScope();
        await scope
            .ServiceProvider.GetRequiredService<RoundExecutionService>()
            .ExecuteAsync(sessionId, secret, new RoundCommand(1000, Guid.NewGuid()), default);
    }

    private sealed class ObservedContextFactory(DbContextOptions<GameDbContext> options)
        : IDbContextFactory<GameDbContext>
    {
        public GameDbContext CreateDbContext() => new(options);
    }

    private sealed class HistoryQueryObserver(Func<Task>? afterFirstQuery = null)
        : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public bool Interleaved { get; private set; }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default
        )
        {
            if (command.CommandText.Contains("\"RoundRecords\"", StringComparison.Ordinal))
            {
                Commands.Add(command.CommandText);
                if (!Interleaved && afterFirstQuery is not null)
                {
                    Interleaved = true;
                    await afterFirstQuery();
                }
            }

            return result;
        }
    }
}
