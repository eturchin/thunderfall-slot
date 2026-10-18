using Microsoft.EntityFrameworkCore;
using Thunderfall.Application;
using Thunderfall.Application.Entities;

namespace Thunderfall.Infrastructure;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<DemoWallet> DemoWallets => Set<DemoWallet>();
    public DbSet<WalletEntry> WalletLedger => Set<WalletEntry>();
    public DbSet<RoundRecord> RoundRecords => Set<RoundRecord>();
    public DbSet<FeatureSession> FeatureSessions => Set<FeatureSession>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<GameSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.Property(x => x.GameId).HasMaxLength(80);
            e.Property(x => x.MathVersion).HasMaxLength(80);
            e.Property(x => x.RulesetHash).HasMaxLength(64);
        });
        b.Entity<DemoWallet>(e =>
        {
            e.HasKey(x => x.SessionId);
            e.HasOne<GameSession>().WithOne().HasForeignKey<DemoWallet>(x => x.SessionId);
            e.ToTable(t => t.HasCheckConstraint("CK_Wallet_Nonnegative", "\"Balance\" >= 0"));
        });
        b.Entity<WalletEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SessionId, x.CreatedAt });
            e.HasOne<GameSession>().WithMany().HasForeignKey(x => x.SessionId);
        });
        b.Entity<RoundRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SessionId, x.CreatedAt });
            e.Property(x => x.OutcomeJson).HasColumnType("jsonb");
            e.HasOne<GameSession>().WithMany().HasForeignKey(x => x.SessionId);
        });
        b.Entity<FeatureSession>(e =>
        {
            e.HasKey(x => x.SessionId);
            e.HasIndex(x => new { x.Active, x.SessionId });
            e.Property(x => x.StateJson).HasColumnType("jsonb");
            e.HasOne<GameSession>().WithOne().HasForeignKey<FeatureSession>(x => x.SessionId);
        });
        b.Entity<IdempotencyRecord>(e =>
        {
            e.HasKey(x => new { x.SessionId, x.Key });
            e.HasOne<RoundRecord>().WithMany().HasForeignKey(x => x.RoundId);
            e.HasOne<GameSession>().WithMany().HasForeignKey(x => x.SessionId);
        });
    }
}
