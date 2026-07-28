using Microsoft.AspNetCore.Mvc;
using Rovio.Matchmaking.Api.Extensions;
using Rovio.Matchmaking.Application.Models;
using Rovio.Matchmaking.Application.Services.Contracts;

namespace Rovio.Matchmaking.Api.Controllers;

[Route("api/v1/sessions")]
public sealed class SessionsController : BaseController
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
        : base(logger) => _sessionService = sessionService;

    [HttpGet("{sessionId}")]
    public async Task<IActionResult> Get(string sessionId, CancellationToken cancellationToken)
    {
        var parsed = sessionId.ParseSessionId();
        if (parsed.IsFailure)
        {
            return HandleError(parsed.Error);
        }

        var result = await _sessionService.GetAsync(parsed.Value, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error);
    }

    [HttpPost("{sessionId}/join")]
    public async Task<IActionResult> LateJoin(
        string sessionId,
        [FromBody] LateJoinRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = sessionId.ParseSessionId();
        if (parsed.IsFailure)
        {
            return HandleError(parsed.Error);
        }

        var result = await _sessionService.LateJoinAsync(parsed.Value, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error);
    }
}
