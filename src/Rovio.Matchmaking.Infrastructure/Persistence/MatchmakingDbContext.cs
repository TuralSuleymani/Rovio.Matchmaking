namespace Rovio.Matchmaking.Infrastructure.Persistence;

public sealed class MatchmakingDbContext(DbContextOptions<MatchmakingDbContext> options) : DbContext(options)
{
    public DbSet<GameMatchConfigEntity> GameMatchConfigs => Set<GameMatchConfigEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<GameMatchConfigEntity>();
        entity.ToTable("game_match_configs");
        entity.HasKey(x => x.GameId);
        entity.Property(x => x.GameId).HasColumnName("game_id").HasMaxLength(128);
        entity.Property(x => x.MinPlayers).HasColumnName("min_players");
        entity.Property(x => x.MaxPlayers).HasColumnName("max_players");
        entity.Property(x => x.AllowLateJoin).HasColumnName("allow_late_join");
        entity.Property(x => x.Enabled).HasColumnName("enabled");
        entity.Property(x => x.MaxQueueDepth).HasColumnName("max_queue_depth");
        entity.Property(x => x.LatencyPolicyJson).HasColumnName("latency_policy_json").HasColumnType("jsonb");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
