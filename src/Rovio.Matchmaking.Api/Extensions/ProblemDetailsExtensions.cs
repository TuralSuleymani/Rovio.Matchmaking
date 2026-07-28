using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Rovio.Matchmaking.Api.Extensions;

public static class ProblemDetailsExtensions
{
    public static ProblemDetails CreateNotFound(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        string? code = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status404NotFound, context, details, code, errors);

    public static ProblemDetails CreateBadRequest(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        string? code = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status400BadRequest, context, details, code, errors);

    public static ProblemDetails CreateConflict(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        string? code = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status409Conflict, context, details, code, errors);

    public static ProblemDetails CreateValidation(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        string? code = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status400BadRequest, context, details, code, errors);

    public static ProblemDetails CreateTooManyRequests(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        string? code = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status429TooManyRequests, context, details, code, errors);

    public static ProblemDetails CreateUnavailable(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        string? code = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status503ServiceUnavailable, context, details, code, errors);

    public static ProblemDetails CreateUnexpected(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        string? code = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status500InternalServerError, context, details, code, errors);

    private static ProblemDetails CreateProblemDetailsWith(
        ProblemDetailsFactory detailsFactory,
        int statusCode,
        HttpContext context,
        string? message,
        string? code,
        IEnumerable<string>? errors)
    {
        var problem = detailsFactory.CreateProblemDetails(
            context,
            statusCode: statusCode,
            detail: message);

        problem.Extensions["code"] = code ?? problem.Title?.ToLowerInvariant() ?? "error";

        if (errors is not null)
        {
            var list = errors as IReadOnlyList<string> ?? errors.ToList();
            if (list.Count > 0)
            {
                problem.Extensions["errors"] = list;
            }
        }

        return problem;
    }
}
