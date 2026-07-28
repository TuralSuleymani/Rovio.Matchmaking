using Ardalis.SmartEnum;

namespace Rovio.Domain.Common.Errors;

public abstract class ErrorType(string name, int value) : SmartEnum<ErrorType>(name, value)
{
    public static readonly ErrorType Conflict = new ConflictEnum();
    public static readonly ErrorType NotFound = new NotFoundEnum();
    public static readonly ErrorType BadRequest = new BadRequestEnum();
    public static readonly ErrorType Validation = new ValidationEnum();
    public static readonly ErrorType Unexpected = new UnexpectedEnum();
    public static readonly ErrorType Unavailable = new UnavailableEnum();
    public static readonly ErrorType TooManyRequests = new TooManyRequestsEnum();

    private sealed class ConflictEnum() : ErrorType("Conflict", 0);
    private sealed class NotFoundEnum() : ErrorType("NotFound", 1);
    private sealed class BadRequestEnum() : ErrorType("BadRequest", 2);
    private sealed class ValidationEnum() : ErrorType("Validation", 3);
    private sealed class UnexpectedEnum() : ErrorType("Unexpected", 4);
    private sealed class UnavailableEnum() : ErrorType("Unavailable", 5);
    private sealed class TooManyRequestsEnum() : ErrorType("TooManyRequests", 6);
}
