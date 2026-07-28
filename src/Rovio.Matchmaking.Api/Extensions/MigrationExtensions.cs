using Microsoft.EntityFrameworkCore;
using Rovio.Matchmaking.Application.Services.Contracts;
using Rovio.Matchmaking.Infrastructure.Persistence;

namespace Rovio.Matchmaking.Api.Extensions;

public static class MigrationExtensions
{
    /// <summary>
    /// Apply migrations at runtime and seed/project matchmaking game configs.
    /// </summary>
    public static async Task ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();
        await db.Database.MigrateAsync();

        var configs = scope.ServiceProvider.GetRequiredService<IGameConfigService>();
        var bootstrap = await configs.EnsureSeededAndProjectedAsync();
        if (bootstrap.IsFailure)
        {
            throw new InvalidOperationException(
                $"Matchmaking bootstrap failed: {bootstrap.Error.Code} - {bootstrap.Error.ErrorMessage}");
        }
    }
}
