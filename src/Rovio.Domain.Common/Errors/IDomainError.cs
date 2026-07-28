namespace Rovio.Domain.Common.Errors;

public interface IDomainError
{
    string? Code { get; }
    string? ErrorMessage { get; }
    ErrorType ErrorType { get; }
    IReadOnlyList<string>? Errors { get; }
}
