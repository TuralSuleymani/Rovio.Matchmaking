using CSharpFunctionalExtensions;
using Rovio.Domain.Common.Errors;

namespace Rovio.Domain.Common;

public sealed record Id<T> : IId, IComparable, IComparable<IId>, IComparable<Guid>, IEquatable<IId>
{
    private Id(Guid value) => Value = value;

    public Guid Value { get; }

    public static Id<T> New() => new(Guid.NewGuid());

    public static Result<Id<T>, DomainError> Create(Guid value)
    {
        if (value == default)
        {
            return DomainError.Validation(
                "Id cannot be empty.",
                code: CommonErrorCodes.InvalidId);
        }

        return new Id<T>(value);
    }

    public static Result<Id<T>, DomainError> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DomainError.BadRequest(
                "Id is required.",
                code: CommonErrorCodes.InvalidId);
        }

        if (!Guid.TryParse(value.Trim(), out var guid) || guid == default)
        {
            return DomainError.BadRequest(
                "Id is invalid.",
                code: CommonErrorCodes.InvalidId);
        }

        return new Id<T>(guid);
    }

    public static Id<T> FromId<TOther>(Id<TOther> id) => new(id.Value);

    public static implicit operator Guid?(Id<T>? id) => id?.Value;
    public static implicit operator Guid(Id<T> id) => id.Value;

    public int CompareTo(object? obj)
    {
        if (obj is IId otherId)
        {
            return CompareTo(otherId);
        }

        if (obj is Guid otherGuid)
        {
            return CompareTo(otherGuid);
        }

        if (obj is null)
        {
            return 1;
        }

        throw new ArgumentException("Object must be of type IId or Guid", nameof(obj));
    }

    public int CompareTo(IId? other) => other?.Value.CompareTo(Value) ?? 1;
    public int CompareTo(Guid other) => Value.CompareTo(other);
    public bool Equals(IId? other) => other?.Value == Value;

    public override string ToString() => Value.ToString("N");
}
