using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Rovio.Domain.Common.Errors;
using Rovio.Matchmaking.Api.Extensions;

namespace Rovio.Matchmaking.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    private readonly Dictionary<ErrorType, Func<IDomainError, ObjectResult>> _errorHandlers;

    protected BaseController(ILogger logger)
    {
        Logger = logger;
        _errorHandlers = new Dictionary<ErrorType, Func<IDomainError, ObjectResult>>
        {
            [ErrorType.Conflict] = ConflictResponse,
            [ErrorType.NotFound] = NotFoundResponse,
            [ErrorType.BadRequest] = BadRequestResponse,
            [ErrorType.Validation] = ValidationResponse,
            [ErrorType.TooManyRequests] = TooManyRequestsResponse,
            [ErrorType.Unavailable] = UnavailableResponse,
            [ErrorType.Unexpected] = UnexpectedResponse
        };
    }

    protected ILogger Logger { get; }

    protected ObjectResult HandleError(IDomainError error)
    {
        if (_errorHandlers.TryGetValue(error.ErrorType, out var handler))
        {
            return handler(error);
        }

        throw new InvalidOperationException($"Unsupported error type: {error.ErrorType}");
    }

    protected IActionResult ToActionResult<T>(
        Result<T, IDomainError> result,
        Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : HandleError(result.Error);

    protected IActionResult ToActionResult(
        UnitResult<IDomainError> result,
        Func<IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : HandleError(result.Error);

    private ObjectResult NotFoundResponse(IDomainError error) =>
        NotFound(ProblemDetailsFactory.CreateNotFound(
            HttpContext, error.ErrorMessage, error.Code, error.Errors));

    private ObjectResult BadRequestResponse(IDomainError error) =>
        BadRequest(ProblemDetailsFactory.CreateBadRequest(
            HttpContext, error.ErrorMessage, error.Code, error.Errors));

    private ObjectResult ConflictResponse(IDomainError error) =>
        Conflict(ProblemDetailsFactory.CreateConflict(
            HttpContext, error.ErrorMessage, error.Code, error.Errors));

    private ObjectResult ValidationResponse(IDomainError error) =>
        BadRequest(ProblemDetailsFactory.CreateValidation(
            HttpContext, error.ErrorMessage, error.Code, error.Errors));

    private ObjectResult TooManyRequestsResponse(IDomainError error) =>
        StatusCode(
            StatusCodes.Status429TooManyRequests,
            ProblemDetailsFactory.CreateTooManyRequests(
                HttpContext, error.ErrorMessage, error.Code, error.Errors));

    private ObjectResult UnavailableResponse(IDomainError error) =>
        StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            ProblemDetailsFactory.CreateUnavailable(
                HttpContext, error.ErrorMessage, error.Code, error.Errors));

    private ObjectResult UnexpectedResponse(IDomainError error) =>
        StatusCode(
            StatusCodes.Status500InternalServerError,
            ProblemDetailsFactory.CreateUnexpected(
                HttpContext, error.ErrorMessage, error.Code, error.Errors));
}
