using Microsoft.AspNetCore.Mvc;
using Rovio.Matchmaking.Api.Extensions;
using Rovio.Matchmaking.Application.Models;
using Rovio.Matchmaking.Application.Services.Contracts;

namespace Rovio.Matchmaking.Api.Controllers;

[Route("api/v1/games/{gameId}/queue")]
public sealed class QueueController : BaseController
{
    private readonly IQueueService _queueService;

    public QueueController(IQueueService queueService, ILogger<QueueController> logger)
        : base(logger) => _queueService = queueService;

    [HttpPost]
    public async Task<IActionResult> Enqueue(
        string gameId,
        [FromBody] EnqueueRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = gameId.ParseGameId();
        if (parsed.IsFailure)
        {
            return HandleError(parsed.Error);
        }

        var result = await _queueService.EnqueueAsync(parsed.Value, request, cancellationToken);
        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        var (ticket, created) = result.Value;
        return created
            ? CreatedAtAction(nameof(GetTicket), new { gameId, ticketId = ticket.TicketId }, ticket)
            : Ok(ticket);
    }

    [HttpGet("{ticketId}")]
    public async Task<IActionResult> GetTicket(
        string gameId,
        string ticketId,
        CancellationToken cancellationToken)
    {
        var parsedGameId = gameId.ParseGameId();
        if (parsedGameId.IsFailure)
        {
            return HandleError(parsedGameId.Error);
        }

        var parsedTicketId = ticketId.ParseTicketId();
        if (parsedTicketId.IsFailure)
        {
            return HandleError(parsedTicketId.Error);
        }

        var result = await _queueService.GetTicketAsync(
            parsedGameId.Value,
            parsedTicketId.Value,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error);
    }

    [HttpDelete("{ticketId}")]
    public async Task<IActionResult> Cancel(
        string gameId,
        string ticketId,
        CancellationToken cancellationToken)
    {
        var parsedGameId = gameId.ParseGameId();
        if (parsedGameId.IsFailure)
        {
            return HandleError(parsedGameId.Error);
        }

        var parsedTicketId = ticketId.ParseTicketId();
        if (parsedTicketId.IsFailure)
        {
            return HandleError(parsedTicketId.Error);
        }

        var result = await _queueService.CancelAsync(
            parsedGameId.Value,
            parsedTicketId.Value,
            cancellationToken);
        return result.IsSuccess ? NoContent() : HandleError(result.Error);
    }
}
