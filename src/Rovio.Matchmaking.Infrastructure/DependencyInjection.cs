namespace Rovio.Matchmaking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMatchmakingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton(configuration);
        services.Configure<MatchmakingOptions>(configuration.GetSection(MatchmakingOptions.SectionName));

        services.AddDbContext<MatchmakingDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            options.UseNpgsql(config.GetConnectionString("Postgres"));
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConnection = config.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.");

            var options = ConfigurationOptions.Parse(redisConnection);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            options.AsyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(options);
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IGameConfigRepository, PostgresGameConfigRepository>();
        services.AddSingleton<IGameConfigProjector, RedisGameConfigProjector>();
        services.AddSingleton<IGameConfigRuntime, RedisGameConfigRuntime>();
        services.AddSingleton<ITicketStore, RedisTicketStore>();
        services.AddSingleton<ISessionStore, RedisSessionStore>();
        services.AddSingleton<IShardLock, RedisShardLock>();
        services.AddScoped<IMatchmakingEngine, MatchmakingEngine>();
        services.AddScoped<IGameConfigService, GameConfigService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<ISessionService, SessionService>();

        return services;
    }
}
