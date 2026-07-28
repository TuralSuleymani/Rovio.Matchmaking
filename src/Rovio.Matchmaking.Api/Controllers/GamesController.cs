using Microsoft.AspNetCore.Mvc;
using Rovio.Matchmaking.Api.Extensions;
using Rovio.Matchmaking.Application.Models;
using Rovio.Matchmaking.Application.Services.Contracts;

namespace Rovio.Matchmaking.Api.Controllers;

[Route("api/v1/games")]
public sealed class GamesController : BaseController
{
    private readonly IGameConfigService _gameConfigService;

    public GamesController(IGameConfigService gameConfigService, ILogger<GamesController> logger)
        : base(logger) => _gameConfigService = gameConfigService;

    [HttpGet]
    public async Task<IActionResult> ListGames(CancellationToken cancellationToken)
    {
        var result = await _gameConfigService.ListGameIdsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error);
    }

    [HttpGet("{gameId}/config")]
    public async Task<IActionResult> GetConfig(string gameId, CancellationToken cancellationToken)
    {
        var parsed = gameId.ParseGameId();
        if (parsed.IsFailure)
        {
            return HandleError(parsed.Error);
        }

        var result = await _gameConfigService.GetAsync(parsed.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error);
    }

    [HttpPut("{gameId}/config")]
    public async Task<IActionResult> UpsertConfig(
        string gameId,
        [FromBody] UpsertGameConfigRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = gameId.ParseGameId();
        if (parsed.IsFailure)
        {
            return HandleError(parsed.Error);
        }

        var result = await _gameConfigService.UpsertAsync(parsed.Value, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error);
    }
}
