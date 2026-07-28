using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Rovio.Matchmaking.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "internal_error",
                Detail = "An unexpected error occurred.",
                Instance = context.Request.Path
            };
            problem.Extensions["code"] = "internal_error";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
