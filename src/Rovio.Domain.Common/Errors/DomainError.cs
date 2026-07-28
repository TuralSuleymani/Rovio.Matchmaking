namespace Rovio.Domain.Common.Errors;

public sealed record DomainError : IDomainError
{
    public static DomainError Conflict(string? message = null, string? code = null) =>
        new(code ?? "conflict", message ?? "The data provided conflicts with existing data.", ErrorType.Conflict, null);

    public static DomainError NotFound(string? message = null, string? code = null) =>
        new(code ?? "not_found", message ?? "The requested item could not be found.", ErrorType.NotFound, null);

    public static DomainError BadRequest(string? message = null, string? code = null) =>
        new(code ?? "bad_request", message ?? "Invalid request or parameters.", ErrorType.BadRequest, null);

    public static DomainError Validation(string? message = null, IReadOnlyList<string>? errors = null, string? code = null) =>
        new(code ?? "validation_failed", message ?? "Validation failed.", ErrorType.Validation, errors);

    public static DomainError Unexpected(string? message = null, string? code = null) =>
        new(code ?? "unexpected", message ?? "Unexpected error happened.", ErrorType.Unexpected, null);

    public static DomainError Unavailable(string? message = null, string? code = null) =>
        new(code ?? "unavailable", message ?? "A required dependency is unavailable.", ErrorType.Unavailable, null);

    public static DomainError TooManyRequests(string? message = null, string? code = null) =>
        new(code ?? "too_many_requests", message ?? "Rate or capacity limit exceeded.", ErrorType.TooManyRequests, null);

    private DomainError(string? code, string? message, ErrorType errorType, IReadOnlyList<string>? errors)
    {
        Code = code;
        ErrorMessage = message;
        ErrorType = errorType;
        Errors = errors;
    }

    public string? Code { get; }
    public string? ErrorMessage { get; }
    public ErrorType ErrorType { get; }
    public IReadOnlyList<string>? Errors { get; }
}
